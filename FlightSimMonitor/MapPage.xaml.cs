using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;
using FlightSimMonitor.Common;
using FlightSimMonitor.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace FlightSimMonitor
{
    public sealed partial class MapPage : Page
    {
        //GeoEvent Extension for Server のストリームサービスの URL を指定します。
        private const string SERVERURL = "ws://localhost/arcgis/ws/services/WSFlight-sim-stream/StreamServer/subscribe?outSR=102100";

        private MessageWebSocket _messageWebSocket;                     //ストリームサービスと接続するための Web ソケット
        private GraphicsLayer _flightsLayer;                            //飛行機レイヤ
        private ObservableCollection<FlightRecord> _flightRecords;      //スコア ランキング上の飛行機のコレクション
        private int _rankingMax = 10;                                   //スコア ランキングに表示する最大機数
        private string _selectedRankerId;                               //選択されたスコア ランキング上の飛行機の ID

        public MapPage()
        {
            this.InitializeComponent();

            //スコア ランキング上の飛行機のコレクションを初期化しランキング ビューのソースに追加
            _flightRecords = new ObservableCollection<FlightRecord>();
            _rankListView.ItemsSource = _flightRecords;

            #region ナビゲーションの登録
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
            #endregion
        }

        /// <summary>
        /// マップビュー読み込み完了時
        /// </summary>
        private async void _mapView_Loaded(object sender, RoutedEventArgs e)
        {
            //すべてのレイヤが読み込まれるまで待機
            await _mapView.LayersLoadedAsync();

            //飛行機レイヤを取得
            _flightsLayer = _mapView.Map.Layers["flights"] as GraphicsLayer;
        }

        #region ストリーム サービスへの接続制御
        /// <summary>
        /// 接続ボタンクリック時
        /// </summary>
        private async void _linkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //GeoEvent Extension for Server のストリームサービスに接続
                MessageWebSocket webSocket = _messageWebSocket;
                if (webSocket == null)
                {
                    Uri server = new Uri(SERVERURL);
                    webSocket = new MessageWebSocket();
                    webSocket.Control.MessageType = SocketMessageType.Utf8;
                    webSocket.MessageReceived += webSocket_MessageReceived;
                    webSocket.Closed += webSocket_Closed;
                    await webSocket.ConnectAsync(server);
                    _messageWebSocket = webSocket;

                    //接続成功を通知
                    var _ = new MessageDialog("サーバーに接続しました。").ShowAsync();
                }
            }
            catch (Exception ex)
            {
                //エラー通知
                var _ = new MessageDialog(ex.Message).ShowAsync();
            }
        }

        /// <summary>
        /// メッセージ受信時
        /// </summary>
        async void webSocket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                using (DataReader reader = args.GetDataReader())
                {
                    //受信したメッセージの読み込み
                    reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                    string read = reader.ReadString(reader.UnconsumedBufferLength);

                    //受信したメッセージをグラフィックに変換
                    var g = Graphic.FromJson(read);

                    //マップ上に同一機が存在するか確認
                    var loggedFlight = _flightsLayer.Graphics.Where(flight => ((string)flight.Attributes[FiledNames.ID]) == ((string)g.Attributes[FiledNames.ID])).FirstOrDefault();

                    //飛行機レイヤに同一機が存在する場合は同一機の情報を更新
                    if (loggedFlight != null)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            //飛行機の現在位置を更新
                            loggedFlight.Geometry = g.Geometry;

                            //飛行方向を更新
                            loggedFlight.Attributes[FiledNames.Angle] = g.Attributes[FiledNames.Angle];

                            //現在のスコアを更新
                            loggedFlight.Attributes[FiledNames.TScore] = g.Attributes[FiledNames.TScore];

                            //シンボルの角度を飛行方向に設定
                            ((PictureMarkerSymbol)loggedFlight.Symbol).Angle = (double)g.Attributes[FiledNames.Angle];

                            //スコアランキングに同一機が存在するか確認
                            var flightRecord = _flightRecords.Where(f => f.ID == (string)loggedFlight.Attributes[FiledNames.ID]).FirstOrDefault();

                            //同一機が存在しスコアが変わっていない場合は何もしない。
                            if (flightRecord != null && flightRecord.TScore == (Int32)g.Attributes[FiledNames.TScore])
                            {
                                return;
                            }

                            //スコアランキングを更新
                            UpdateScoreRanking(g);
                        });
                    }
                    //飛行機レイヤに同一機が存在しない場合は新規追加
                    else
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            //青飛行機シンボルを作成
                            var flightSymbol = await GenerateBlueSymbol();

                            //シンボルの角度を飛行方向に設定
                            flightSymbol.Angle = (double)g.Attributes[FiledNames.Angle];

                            //シンボルをグラフィックに追加
                            g.Symbol = flightSymbol;

                            //選択フラグ属性列をグラフィックに追加
                            g.Attributes.Add(new System.Collections.Generic.KeyValuePair<string, object>(FiledNames.IsSelected, 0));

                            //飛行機レイヤに追加
                            _flightsLayer.Graphics.Add(g);

                            //スコアランキングを更新
                            UpdateScoreRanking(g);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// ストリーム サービスへの接続終了時
        /// </summary>
        void webSocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            MessageWebSocket webSocket = Interlocked.Exchange(ref _messageWebSocket, null);
            if (webSocket != null)
            {
                webSocket.Dispose();
            }
        }
        #endregion

        /// <summary>
        /// リフレッシュボタン クリック時
        /// </summary>
        private void _refreshButton_Click(object sender, RoutedEventArgs e)
        {
            //飛行機レイヤのグラフィックをクリア
            _flightsLayer.Graphics.Clear();

            //スコアランキングをクリア
            _flightRecords.Clear();
        }

        #region ランキング 制御
        /// <summary>
        /// スコア ランキングの更新
        /// </summary>
        private void UpdateScoreRanking(Graphic g)
        {
            try
            {
                //現在のスコアを取得
                Int32 tScore = (Int32)g.Attributes[FiledNames.TScore];

                //ランキングに最大機数が登録されいる場合
                if (_flightRecords.Count > _rankingMax - 1)
                {
                    //ランクの最低スコアよりスコアが低いもしくは同じであれば登録しない
                    if (tScore <= _flightRecords[_flightRecords.Count - 1].TScore)
                    {
                        return;
                    }
                    //ランキングにインサートし最下位を削除
                    else
                    {
                        //（存在する場合は）ランキングから同一機を削除
                        RemoveSelf(g);

                        //スコア ランキングの順位を取得
                        int rank = GetRank(tScore);

                        //スコア ランキングにインサート
                        AddToRanking(g, rank);

                        //スコア ランキングの最下位を削除
                        _flightRecords.RemoveAt(_flightRecords.Count - 1);
                    }
                }
                //ランキングに最大機数が登録されていない場合
                else
                {
                    //（存在する場合は）ランキングから同一機を削除
                    RemoveSelf(g);

                    //スコア ランキングの順位を取得
                    int rank = GetRank(tScore);

                    //スコア ランキングに追加
                    AddToRanking(g, rank);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// ランキングに同一機が存在する場合は自分を削除
        /// </summary>
        private void RemoveSelf(Graphic g)
        {
            //飛行機の ID を取得
            string id = (string)g.Attributes[FiledNames.ID];

            //スコア ランキングに同一機が存在するか確認
            var self = _flightRecords.Where(f => f.ID == id).FirstOrDefault();

            //存在する場合はスコア ランキングから同一機を削除
            if(self != null)
            {
                _flightRecords.Remove(self);
            }
        }

        /// <summary>
        /// スコア ランキングの順位を取得
        /// </summary>
        private int GetRank(Int32 tScore)
        {
            int rank = 0;

            try
            {
                for (rank = 0; rank < _flightRecords.Count; rank++)
                {
                    if (tScore > _flightRecords[rank].TScore)
                    {
                        return rank;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return rank;
        }

        /// <summary>
        /// ランキングへレコードを追加
        /// </summary>
        private void AddToRanking(Graphic g, int rank)
        {
            //スコア ランキング アイテムを作成
            var flightRecord = new FlightRecord()
            {
                ID = (string)g.Attributes[FiledNames.ID],
                Name = (string)g.Attributes[FiledNames.Name],
                TScore = (Int32)g.Attributes[FiledNames.TScore],
                SymbolPath = "ms-appx:///Assets/plane_blue.png",
            };

            //スコア ランキングに追加
            if(rank < 0)
            {
                _flightRecords.Add(flightRecord);
            }
            else
            {
                _flightRecords.Insert(rank, flightRecord);
            }
        }

        /// <summary>
        /// スコア ランキングのアイテム選択変更時
        /// </summary>
        private async void _rankListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //前回選択された飛行機が存在する場合は飛行機のシンボルを青飛行機シンボルに修正
            if(!string.IsNullOrEmpty(_selectedRankerId))
            {
                //選択された飛行機を取得
                var g = _flightsLayer.Graphics.Where(f => _selectedRankerId == (string)f.Attributes[FiledNames.ID]).FirstOrDefault();

                //飛行機のシンボルを青飛行機シンボルに修正
                if (g != null)
                {
                    //未選択状態に変更
                    g.Attributes[FiledNames.IsSelected] = 0;

                    //青飛行機シンボルを作成
                    var flightSymbol = await GenerateBlueSymbol();

                    //シンボルの角度を飛行方向に設定
                    flightSymbol.Angle = (double)g.Attributes[FiledNames.Angle];

                    //飛行機にシンボルを設定
                    g.Symbol = flightSymbol;
                }
            }

            //新規に選択された機体のシンボルを赤飛行機シンボルに修正
            var flightRecords = e.AddedItems.FirstOrDefault() as FlightRecord;
            if(flightRecords != null)
            {
                //選択された飛行機の ID を保持
                _selectedRankerId = flightRecords.ID;

                //選択された飛行機を取得
                var g = _flightsLayer.Graphics.Where(f => _selectedRankerId == (string)f.Attributes[FiledNames.ID]).FirstOrDefault();
                if(g != null)
                {
                    //選択状態に変更
                    g.Attributes[FiledNames.IsSelected] = 1;

                    //赤飛行機シンボルを作成
                    var flightSymbol = await GenerateRedSymbol();

                    //シンボルの角度を飛行方向に設定
                    flightSymbol.Angle = (double)g.Attributes[FiledNames.Angle];

                    //飛行機にシンボルを設定
                    g.Symbol = flightSymbol;

                    //選択された飛行機を中心に地図を移動
                    await _mapView.SetViewAsync(g.Geometry);
                }
            }
            else
            {
                //選択された飛行機の ID を削除
                _selectedRankerId = string.Empty;
            }
        }
        #endregion

        #region シンボルの生成
        /// <summary>
        /// 飛行機（青）シンボルの生成
        /// </summary>
        private async Task<PictureMarkerSymbol> GenerateBlueSymbol()
        {
            var flightSymbol = new PictureMarkerSymbol()
            {
                Width = 20,
                Height = 20,
            };

            await flightSymbol.SetSourceAsync(new Uri("ms-appx:///Assets/plane_blue.png"));

            return flightSymbol;
        }

        /// <summary>
        /// 飛行機（赤）シンボルの生成
        /// </summary>
        private async Task<PictureMarkerSymbol> GenerateRedSymbol()
        {
            var flightSymbol = new PictureMarkerSymbol()
            {
                Width = 26,
                Height = 26,
            };

            await flightSymbol.SetSourceAsync(new Uri("ms-appx:///Assets/plane_red.png"));

            return flightSymbol;
        }
        #endregion

        #region 自動生成されたナビゲーション制御用コード
        private NavigationHelper navigationHelper;

        /// <summary>
        /// NavigationHelper は、ナビゲーションおよびプロセス継続時間管理を
        /// 支援するために、各ページで使用します。
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// このページには、移動中に渡されるコンテンツを設定します。前のセッションからページを
        /// 再作成する場合は、保存状態も指定されます。
        /// </summary>
        /// <param name="sender">
        /// イベントのソース (通常、<see cref="NavigationHelper"/>)>
        /// </param>
        /// <param name="e">このページが最初に要求されたときに
        /// <see cref="Frame.Navigate(Type, Object)"/> に渡されたナビゲーション パラメーターと、
        /// 前のセッションでこのページによって保存された状態の辞書を提供する
        /// セッション。ページに初めてアクセスするとき、状態は null になります。</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// アプリケーションが中断される場合、またはページがナビゲーション キャッシュから破棄される場合、
        /// このページに関連付けられた状態を保存します。値は、
        /// <see cref="SuspensionManager.SessionState"/> のシリアル化の要件に準拠する必要があります。
        /// </summary>
        /// <param name="sender">イベントのソース (通常、<see cref="NavigationHelper"/>)</param>
        /// <param name="e">シリアル化可能な状態で作成される空のディクショナリを提供するイベント データ
        ///。</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper の登録

        /// このセクションに示したメソッドは、NavigationHelper がページの
        /// ナビゲーション メソッドに応答できるようにするためにのみ使用します。
        /// 
        /// ページ固有のロジックは、
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// および <see cref="GridCS.Common.NavigationHelper.SaveState"/> のイベント ハンドラーに配置する必要があります。
        /// LoadState メソッドでは、前のセッションで保存されたページの状態に加え、
        /// ナビゲーション パラメーターを使用できます。

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
        #endregion
    }
}
