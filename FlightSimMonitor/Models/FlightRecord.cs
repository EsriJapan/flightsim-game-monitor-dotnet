using System;

namespace FlightSimMonitor.Models
{
    /// <summary>
    /// スコア ランキング表示用の飛行レコード クラス
    /// </summary>
    public class FlightRecord
    {
        /// <summary>
        /// フライトID
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// パイロット名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// トータル スコア
        /// </summary>
        public Int32 TScore { get; set; }

        /// <summary>
        /// シンボルへのパス
        /// </summary>
        public string SymbolPath { get; set; }
    }
}
