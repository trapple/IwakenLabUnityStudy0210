
## 1.MainFlow.csを読みやすく

上から下に処理を追えばよい形にする

1. TutorialGameObjectをactiveにする
2. チュートリアル処理を開始し、終わるのを待つ
3. TutorialGameObjectをactive=falseにする

💡 RxではなくTaskを使う

## 2.TutorialFlow.RunTutorialAsyncを読みやすく

上から下に処理を追えばよい形にする
入れ子を減らす

1. 剣を描けテキスト表示 
2. 剣が描かれるのを待つ
3. ぶった斬れテキスト表示
4. ボール生成開始
5. 剣でボールを切る
6. 剣がボールにHitしたらチュートリアル終了

## 3.WeaponDrawSequencer内の責務を分ける

### コンストラクタ

- クラスの構築
- 副作用のある処理を描かない

### 理由

- テストしにくくなる
  - new した瞬間に購読が始まると、テストでインスタンスを作るだけで副作用が発生する。
- タイミングを制御出来ない
- 失敗の制御がしづらい

## 4.メモリーリークに注意

非同期はメモリーリークしやすい

- await中にインスタンスが破棄された
- 処理がキャンセルされた

例）コミットハッシュ 2b1e97e2c6de8dc9f53c9f153e6ca9591d81d695 のケース

- WeaponDrawSequencerでマウスが左クリック中にインスタンスが破棄されたら？
    - awaitが残り続ける
- TutorialFlowで _battleWeaponInstance.OnHitを待ってる時にcancelが飛んできたら？
    - _ballSpawnSubscription?.Dispose();が呼ばれず処理が走り続ける