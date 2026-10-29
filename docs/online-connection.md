# 2人接続・移動同期の確認

この段階では、接続と移動だけを別シーンで確認します。GameScene の1人用ゲームはそのままです。
オンラインの爆弾、鬼、ダメージ、アイテム、勝敗の同期は次の段階です。

## 最初の準備

1. Windows側で `feature/bomb-inventory` を Pull します。Unityは再生を止め、編集中のシーンを保存してください。
2. Unity 6000.3.10f1 に戻り、Package Managerのダウンロードとコンパイルが終わるまで待ちます。
3. `Tools → Bomb Tag → Create or Open Online Test` を選びます。
4. `Assets/Online/Generated/OnlineTest.unity` とプレイヤーのプレハブが作成されます。既にある場合はシーンを開くだけで上書きしません。
5. ▶を押すと接続メニューが表示されます。

生成したシーンはビルド設定の末尾に追加されます。既存シーンの順番は変えません。
下記の専用ビルドメニューは OnlineTest だけをビルドするため、SampleScene が先頭でも接続確認を行えます。
生成されたシーン、プレハブ、マテリアルと .meta は、確認後にGitへ追加すれば他のPCでも共有できます。

## Unity Cloud未連携でもできる、同じPCでの確認

1. 再生を止めて `Tools → Bomb Tag → Build Windows Connection Test` を選びます。
2. Assetsの外の出力フォルダー（例：プロジェクト直下のBuilds/OnlineTest）を選び、ビルド完了を待ちます。
3. 出力された `BombTagOnline.exe` を起動します。
4. Unity Editorでも OnlineTest を再生します。
5. Editorの `Same-PC test` を選び、`Local Host` を押します。
6. exe側も `Same-PC test` を選び、`Local Join` を押します。
7. 2人のキャラクターが現れ、ホスト側に Players: 2/2 と表示されることを確認します。
8. 操作したい画面をクリックしてWASDか矢印で移動します。自分だけが操作され、両画面で移動が見えることを確認します。

Local接続は127.0.0.1:7777に限定しています。別のPCの友達とは次の参加コード接続を使います。
Editorとexeは、非アクティブな間も通信を継続しますが、移動入力はフォーカス中の画面だけが送ります。

## 友達と参加コードで接続する準備

1. Unity EditorでUnity IDにログインします。
2. `Edit → Project Settings → Services` でUnity Cloudのプロジェクトに連携します。
   表示が異なる場合はUnityのServicesメニューやUnity Cloudダッシュボードから同じプロジェクトを開きます。
3. Unity Cloud側で、そのプロジェクトのMultiplayer Services（Sessions / Lobby / Relay）とAuthenticationを利用できる状態にします。
   規約などの初回設定が出た場合はプロジェクト所有者が設定します。
4. 連携後にWindowsビルドを作り直します。同じCloud Project IDを含む同じビルド一式を友達に渡してください。
   exeだけでなく、出力フォルダー全体（DataフォルダーやDLLを含む）が必要です。
5. ホストが `Host online room` を押し、表示された参加コードを友達に伝えます。
6. 友達がコードを入力し、`Join using code` を押します。

個々のプレイヤーがUnity IDを入力する必要はありません。ゲーム内では匿名認証を使用します。
認証用の秘密鍵をソースやチャットへ入力する必要もありません。
Editorとexeでは認証プロファイルを分けているため、同じPCでも別プレイヤーとして参加できます。
同じPCでexeを2個開くケースの認証プロファイル分離は対象外です。
ホスト退出時の自動引き継ぎはありません。退出後は新しい部屋を作ってください。

## 合格を確認する項目

- 2人が別々の場所に出現し、自分のキャラクターだけ操作できる。
- 斜め移動で速度が増えず、マップ外へ出られない。
- 3人目は参加できない。
- ゲストがDisconnectするとキャラクターが消え、同じ部屋へ再参加できる。
- ホストがDisconnectするとゲストも切断され、メニューから再接続できる。
- 接続先がいないLocal Joinは失敗またはタイムアウトし、やり直せる。
- 間違った参加コードを入力した場合はエラーを表示し、入力し直せる。
- 切断後にLocal接続と参加コード接続を切り替えられる。
- Consoleに赤いエラーがない。

## 自動テストと検証状況

`Window → General → Test Runner → EditMode → Run All` で ConnectionRulesTests の6件を実行できます。
人数制限、参加枠の再利用、重複承認、部屋の初期化、移動速度の制限、不正な入力を確認します。
実際の2プロセス接続は上記の手順で別途確認が必要です。

クラウドにはUnity Editorがなく、Unityパッケージ配信先への接続も制限されているため、
Unityでのパッケージ解決、コンパイル、EditModeテスト、ビルド、実通信は未検証です。
C#構文と依存定義は静的にチェックしています。
依存バージョンと追加したlockfileのメタデータはUnity公式Boss Roomサンプルの公開内容を参照しています。
Unityが必要に応じてlockfileを再生成した場合は、実際の解決結果を確認して一緒に保存してください。

参照：
- https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop
- https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/tree/v2.4.3
