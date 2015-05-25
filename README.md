# flightsim-game-monitor-dotnet

Web Socket を利用した双方向通信によりマルチユーザー プレイを実現したフライト シミュレーター ゲームのスコア モニター用アプリです。
フライト シミュレーター ゲームの機体を動かしてゲームをプレイできる[コントローラー用のWeb アプリ](https://github.com/esrijapan/flightsim-game-controller-js)と連携します。

※公開ソースコードを動作させてマルチユーザー プレイと加点減点処理を行うには独自に Web Socket サーバー（[ArcGIS GeoEvent Extension for Server](http://www.esrij.com/products/arcgis-for-server/details/arcgis-geoevent-extension-for-server/)）を設置する必要があります。

![フライト シミュレーター ゲームのスコア モニター用アプリ](_readme_images/app_image1.png)

## 使用している製品・プロジェクト

* [ArcGIS Runtime SDK for .NET](https://developers.arcgis.com/net/)
* [ArcGIS for Developers](https://developers.arcgis.com/en/)
* [ArcGIS GeoEvent Extension for Server](https://server.arcgis.com/ja/geoevent-extension/)

**ArcGIS の開発キットを使用して開発を行う場合は ArcGIS Online 開発者アカウント（[ArcGIS for Developers](https://developers.arcgis.com/en/)）が必要です。開発者アカウントは無償で作成することができます。作成方法は[こちら](http://www.esrij.com/cgi-bin/wp/wp-content/uploads/documents/signup-esri-developers.pdf)**

## 動作環境
###OS
* Windows 8.1

###開発環境
* Microsoft Visual Studio 2013
* Microsoft Visual Studio Express 2013 for Windows

## リソース

* [GeoNet開発者コミュニティ サイト](https://geonet.esri.com/groups/devcom-jp)
* [ArcGIS Runtime SDK for .NET(ESRIジャパン)](http://www.esrij.com/products/arcgis-runtime-sdk-for-dotnet/)
* [ArcGIS Runtime SDK for .NET リファレンス](https://developers.arcgis.com/net/desktop/api-reference/)

##制限事項
既知の問題により、ソリューションを正しくデバッグ実行するには、ソリューションのクローンもしくはダウンロード先のディレクトリパスに日本語などの 2 バイト文字を含めないでください（※ ArcGIS Runtime SDK for .NET を別途ご使用のマシンにインストールする場合は、この制限事項は適用されません）。

## ライセンス
Copyright 2015 Esri Japan Corporation.

Apache License Version 2.0（「本ライセンス」）に基づいてライセンスされます。あなたがこのファイルを使用するためには、本ライセンスに従わなければなりません。本ライセンスのコピーは下記の場所から入手できます。

> http://www.apache.org/licenses/LICENSE-2.0

適用される法律または書面での同意によって命じられない限り、本ライセンスに基づいて頒布されるソフトウェアは、明示黙示を問わず、いかなる保証も条件もなしに「現状のまま」頒布されます。本ライセンスでの権利と制限を規定した文言については、本ライセンスを参照してください。

ライセンスのコピーは本リポジトリの[ライセンス ファイル](./LICENSE)で利用可能です。

[](EsriJapan Tags: <Windows> )
[](EsriJapan Language: <C#>)
