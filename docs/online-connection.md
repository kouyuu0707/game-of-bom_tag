# オンライン協力プレイの確認

OnlineTestでは、2人の移動・爆弾・鬼・HP・勝敗をホスト側で管理し、結果を同期します。
GameSceneの1人用ゲームはそのままです。オンラインの補充アイテムはまだありません。

## 更新して起動する

1. Editorの再生を停止し、起動中の古いexeを終了します。編集中のシーンは保存します。
2. Windows側で `feature/bomb-inventory` をPullし、Unityの読み込み・コンパイル完了を待ちます。
3. `Tools → Bomb Tag → Create or Open Online Test` を実行します。
4. 既存のOnlineTestの配置を保ち、HP・鬼・ラウンド用プレハブと地面のNavMeshSurfaceを追加・接続します。
5. `Tools → Bomb Tag → Build Windows Connection Test` でWindowsビルドを作り直します。
   Assetsの外（例：プロジェクト直下のBuilds/OnlineTest）を出力先に選びます。
6. 出力された `BombTagOnline.exe` を起動し、EditorでもOnlineTestを再生します。
7. Editorで `Same-PC test → Local Host`、exeで `Same-PC test → Local Join` を選びます。
8. 2人がそろったら、ホスト側で **Start game** を押します。

同じPCの接続にはUnity Cloud連携は不要です。127.0.0.1:7777を使用します。
Editorとexeは必ず同じ版を使ってください。古いexeでは新しいNetworkBehaviourの構成と一致しません。
操作したい画面をクリックすると、その画面のプレイヤーだけ入力を受け付けます。
生成したプレハブ・シーン・マテリアルと .meta は、確認後にGitへ追加すれば他のPCでも共有できます。
専用ビルドメニューはOnlineTestだけをビルドするので、既存シーンの並び順は変えません。

## 現在のルール

- 2人接続後、ホストが開始するまで移動・設置・起爆を待機します。
- WASDか矢印キーで移動、Bで爆弾を設置、Eで自分の設置済み爆弾を全部起爆します。
- 爆弾は各プレイヤー開始時2個、最大5個。設置で1個消費し、起爆しても戻りません。
- プレイヤーはHP3。爆風や鬼との接触で1ダメージ、被弾後1秒間は無敵です。
- 無敵中は体が点滅し、HP0で灰色になります。倒れた人は移動・設置・起爆できません。
- 赤い鬼2体は、近い生存プレイヤーをNavMeshで追いかけます。爆風1回で倒せます。
- 爆風は半径3の球形判定。自分・味方にも当たり、同じ爆風で複数Colliderに触れても1回分です。
- 1人が倒れても仲間が生存していれば続行します。
- 鬼の全滅でGame Clear!、全員のHP0でGame Overです。同じ爆発で両方全滅した場合はGame Overです。
- 終了後は移動・設置・起爆と鬼の追跡を止め、ホスト側にRetryを表示します。
- Retryは同じ部屋で全員のHP3・爆弾2・開始位置・鬼2体に戻します。
- 進行中の途中参加は拒否します。終了後は参加でき、2人がそろえばRetryできます。
- 進行中にゲストが切断した場合、ホストは続行できます。ホストが終了すると部屋は切断されます。

鬼の出現位置は `Assets/Online/Generated/OnlineRound.prefab` のEnemy Positionsで調整できます。
地面のNavMeshは開始時にホストが生成します。出現位置は歩行可能領域内にしてください。
壁による爆風遮断、誘爆、復活・救助、ホスト引き継ぎは未実装です。

## 2画面で確認する項目

- 接続中は双方にHP3と残弾2が表示され、Start gameを押すまで鬼が出現しない。
- 開始後、赤い鬼が両画面に2体現れ、近い生存プレイヤーへ向かう。
- ホストのBで青い爆弾、ゲストのBでオレンジの爆弾が両画面に現れる。
- Eで自分の爆弾だけ消え、相手の爆弾が残る。起爆しても所持数は戻らない。
- 爆弾から離れて起爆するとダメージを受けず、近くで起爆するとHPが1減る。
- 自分・味方のどちらの爆弾でもHPが減り、そのHPが両画面で一致する。
- 爆弾を2個同時に起爆しても、1秒の無敵時間内はダメージが重複しない。
- 鬼との接触でHPが1減り、触れ続けると無敵時間が終わるたびに減る。
- HP0の人が灰色になって操作できなくなるが、仲間が生存している間は終了しない。
- 鬼が倒れると両画面から消え、残りの鬼の数も一致する。
- 鬼全滅／全員死亡の結果が両画面で一致する。
- ホストのRetryで両画面のHP・残弾・設置数・鬼・位置が初期状態へ戻る。
- 進行中の再参加は拒否され、ラウンド終了後に再参加してからRetryできる。
- Disconnect後に新しい部屋を作っても、以前の鬼や爆弾が残らない。
- Consoleに新しい赤いエラーがない。

## 友達と参加コードで接続する

1. Unity EditorでUnity IDにログインします。
2. `Edit → Project Settings → Services` でUnity Cloudのプロジェクトに連携します。
   表示が異なる場合はServicesメニューやUnity Cloudダッシュボードから同じプロジェクトを開きます。
3. Cloud側で、そのプロジェクトのMultiplayer Services（Sessions / Lobby / Relay）とAuthenticationを利用できる状態にします。
   初回の規約や設定があればプロジェクト所有者が設定します。
4. 連携後にビルドし直し、同じビルドの出力フォルダー全体を友達に渡します（exeだけでは動きません）。
5. ホストが `Host online room` を押し、表示されたコードを友達へ伝えます。
6. 友達がコードを入力し、`Join using code` を押します。
7. 2人接続後、ホストがStart gameを押します。

ゲーム内では匿名認証を使うため、各プレイヤーがUnity IDを入力する必要はありません。
秘密鍵をソースやチャットへ入力する必要もありません。
Editorとexeは別の認証プロファイルを使用します。同じPCでexeを2個開く認証プロファイル分離は対象外です。

## 自動テストと検証状況

Unityでは `Window → General → Test Runner → EditMode → Run All` で次の21件を実行できます。

- ConnectionRulesTests：6件（人数制限、参加枠の再利用、入力の制限など）
- BombStockTests：4件（初期所持数、個別管理、残弾0、最大5個）
- CombatRulesTests：11件（HP、無敵時間、死亡、リセット、勝敗と同時全滅）

Unityを必要としない所持数・HP・勝敗の15件は、同じテストソースを .NET 8 でも実行できます。
リポジトリのルートで `dotnet run --project tools/logic-tests/LogicTests.csproj --configuration Release` を実行します。
この15件はクラウドで実行して全件成功しました。C#の構文も静的チェック済みです。

今回のオンライン戦闘について、クラウドでのUnityコンパイル、NavMesh生成、2プロセス通信の検証は未実施です。
.NETの成功はUnityのコンパイルや実通信の成功を示すものではありません。上記の2画面確認を別途行ってください。

参照：
- https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop
- https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/tree/v2.4.3
