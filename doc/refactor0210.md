# 仕事で使えるUnity勉強会

Iwaken Lab.  
2026/2/10

---

## 自己紹介

<img src="https://pbs.twimg.com/profile_images/1026628428928172032/n-IqZ2wr_400x400.jpg"> 

### まっすー。

- Unity歴だいたい10年
- Ex- HADO Tech Lead
- その前はWeb Full Stackだいたい10年

## 仕事でどんなことが求められるか

- チーム開発
- 保守性の高さ 
  - バグがない
    - あっても追いかけやすい
  - １つのプロダクトと長く付き合っていく
     - 拡張しやすい
     - 読みやすい <-- 今日はここ💡
- 特化した専門性
- 開発スピード

## 読みやすいコード

- チームメンバーがすぐ理解出来る
- 半年後の自分がすぐ理解出来る
- できる限りシンプルに保つ
  - 各クラスの責任範囲が明確（狭い）
  - 上から下に読める

## サンプルコード

https://github.com/trapple/IwakenLabUnityStudy0210

ゲーム内容
「一筆書きで剣を描いて敵を斬る」ゲームのチュートリアル部分です。

- マウスで一筆書きして剣を描く
- 描いた剣がそのまま武器になる
- WASD/矢印キーで移動、スペースキーで振る
- 落ちてくるボールを斬ってチュートリアルクリア

コード提供 : Sketch Knights

## refactor-1 MainFlowのリファクタリング

```csharp
  // MainFlow.cs
  void OnEnable()
  {
      tutorialFlow.FinishTutorial.Subscribe(_ => // (1)
      {
          tutorialFlow.gameObject.SetActive(false); // (3)
      });
      tutorialFlow.gameObject.SetActive(true); // (2)
  }
```

```csharp
  // TutorialFlow.cs
  private readonly Subject<Unit> _finishTutorial = new ();
  public Observable<Unit> FinishTutorial => _finishTutorial; // (6)

  private void Awake() 
  {
      // 武器を描くクラス生成 // (1)
      _ovrWeaponDraw = new WeaponDrawSequencer(mouseInputObserver, drawWeapon);

      // 武器が描かれるのを待つ // (2)
      _ovrWeaponDraw.OnDrawEnd.Subscribe(nodes => 
      {
          // (3)
          text.text = "Spaceキーで剣を振ってぶった斬れ！";

          _battleWeaponInstance = Instantiate(battleWeaponPrefab);
          _battleWeaponInstance.Initialize(nodes);

          // ボールを定期的に生成
          StartBallSpawning();

          // 武器がボールに当たるのを待つ (4)
          _battleWeaponInstance.OnHit.Subscribe(col =>
          {
              if (!col.gameObject.CompareTag("StartObject")) return;
              _finishTutorial.OnNext(Unit.Default); // (5)
              text.text = "チュートリアルクリア！";
          }).AddTo(this);
      }).AddTo(this);
  }
```

## 上から下に処理を追えばよい形にする

1. TutorialGameObjectをactiveにする
2. チュートリアル処理を開始し、終わるのを待つ
3. TutorialGameObjectをactive=falseにする

💡 RxではなくTaskを使う

```csharp
  private async void OnEnable()
  {
      tutorialFlow.gameObject.SetActive(true); // (1)
      await tutorialFlow.RunTutorialAsync(); // (2)
      tutorialFlow.gameObject.SetActive(false); // (3)
  }
```

```csharp
  private readonly Subject<Unit> _finishTutorial = new (); // (8)

  public async UniTask RunTutorialAsync(CancellationToken token)
  {
      // 剣を描けテキスト表示 (1)
      text.text = "一筆書きで剣を描け！";

      _ovrWeaponDraw = new WeaponDrawSequencer(mouseInputObserver, drawWeapon);

      // 剣が描かれるのを待つ // (2)
      _ovrWeaponDraw.OnDrawEnd.Subscribe(nodes =>
      {
          //ぶった斬れテキスト表示 (4)
          text.text = "Spaceキーで剣を振ってぶった斬れ！";

          _battleWeaponInstance = Instantiate(battleWeaponPrefab);
          _battleWeaponInstance.Initialize(nodes);

          // ボール生成開始 (5)
          StartBallSpawning();

          // 剣でボールを切る (6)
          _battleWeaponInstance.OnHit.Subscribe(col =>
          {
              if (!col.gameObject.CompareTag("StartObject")) return;
               Destroy(col.gameObject);
              _finishTutorial.OnNext(Unit.Default); // 剣がボールにHitしたらチュートリアル終了 (7)
          }).AddTo(this);
      }).AddTo(this);

      // (3) チュートリアル処理が終わるのを待つ
      await _finishTutorial.FirstAsync(token); // (9) 終わった！
      text.text = "チュートリアルクリア！";
  }
```

## refactor-2 TutorialFlow.RunTutorialAsyncのリファクタリング

上から下に処理を追えばよい形にする
入れ子を減らす

1. 剣を描けテキスト表示 
2. 剣が描かれるのを待つ
3. ぶった斬れテキスト表示
4. ボール生成開始
5. 剣でボールを切る
6. 剣がボールにHitしたらチュートリアル終了

```csharp
  // TutorialFlow.cs refactor-2-1
  private readonly Subject<Unit> _finishTutorial = new (); // (8)

  public async UniTask RunTutorialAsync(CancellationToken token)
  {
      // 剣を描けテキスト表示 (1)
      text.text = "一筆書きで剣を描け！";

      _ovrWeaponDraw = new WeaponDrawSequencer(mouseInputObserver, drawWeapon);

      // 剣が描かれるのを待つ (2)
      var nodes = await _ovrWeaponDraw.OnDrawEnd.FirstAsync(token);

      // ぶった斬れテキスト表示 (3)
      text.text = "Spaceキーで剣を振ってぶった斬れ！";

      _battleWeaponInstance = Instantiate(battleWeaponPrefab);
      _battleWeaponInstance.Initialize(nodes);

      // ボール生成開始 (4)
      StartBallSpawning();

      // 武器がボールに当たるのを待つ (5)
      _battleWeaponInstance.OnHit.Subscribe(col =>
      {
          if (!col.gameObject.CompareTag("StartObject")) return;
          Destroy(col.gameObject);
          _finishTutorial.OnNext(Unit.Default); // (7)
      }).AddTo(this);

      // チュートリアル処理が終わるのを待つ // (6)
      await _finishTutorial.FirstAsync(token); // (9) 終わった！
      text.text = "チュートリアルクリア！";
  }
```

```csharp
  // TutorialFlow.cs refactor-2-2
  public async UniTask RunTutorialAsync(CancellationToken token)
  {
      // 剣を描けテキスト表示 (1)
      text.text = "一筆書きで剣を描け！";

      _ovrWeaponDraw = new WeaponDrawSequencer(mouseInputObserver, drawWeapon);

      // 剣が描かれるのを待つ (2)
      var nodes = await _ovrWeaponDraw.OnDrawEnd.FirstAsync(token);

      // ぶった斬れテキスト表示 (3)
      text.text = "Spaceキーで剣を振ってぶった斬れ！";

      _battleWeaponInstance = Instantiate(battleWeaponPrefab);
      _battleWeaponInstance.Initialize(nodes);

      // ボール生成開始 (4)
      StartBallSpawning();

      // 武器がボールに当たるのを待つ (5)
      var col = await _battleWeaponInstance.OnHit
          .Where(col => col.gameObject.CompareTag("StartObject"))
          .FirstAsync(token);

      // 武器がボールに当たるのを待つ (6)
      text.text = "チュートリアルクリア！";
      Destroy(col.gameObject);
  }
```

## 3.WeaponDrawSequencer内の責務を分ける

```csharp
  // WeaponDrawSequencer.cs refactor-3
  private readonly Subject<Vector3[]> _onDrawEnd = new();
  public Observable<Vector3[]> OnDrawEnd => _onDrawEnd; // (7) 終わり
        
  public WeaponDrawSequencer(MouseInputObserver mouseInput, DrawWeapon weapon)
  {
      // マウス左クリックで描画 (1)
      mouseInput.LeftClick.Subscribe(value =>
      {
          if (value) // 押された (2)
          {
              var mousePosition = mouseInput.MouseWorldPosition.CurrentValue;
              weapon.DrawStart(mousePosition); // 描画開始 (3)

              // 毎フレームマウスの現在地点を取得 (4)
              _touchSubscription = Observable.EveryUpdate().Subscribe(_ =>
              {
                  var deltaTime = Time.deltaTime;
                  var pos = mouseInput.MouseWorldPosition.CurrentValue;

                  weapon.Draw(pos, deltaTime);
              });
          }
          else // 離した (5)
          {
              _touchSubscription?.Dispose();
              var data = weapon.DrawEnd();
              if (data != null) _onDrawEnd.OnNext(data); // (6) データがあったら終わり
          }
      }).AddTo(_compositeDisposable);
  }
```

### コンストラクタ

- クラスの構築
- 副作用のある処理を描かない

### 理由

- テストしにくくなる
  - new した瞬間に購読が始まると、テストでインスタンスを作るだけで副作用が発生する。
- タイミングを制御出来ない
- 失敗の制御がしづらい

```csharp
  // WeaponDrawSequencer.cs
  public WeaponDrawSequencer(MouseInputObserver mouseInput, DrawWeapon weapon)
  {
      _mouseInput = mouseInput;
      _weapon = weapon;
  }

  public async UniTask<Vector3[]> WaitForDrawEndAsync(CancellationToken cancellation)
  {
      while (true)
      {
          // マウス左クリックで描画 (1)
          await _mouseInput.LeftClick
              .Where(pressed => pressed) // 押された(2)
              .FirstAsync(cancellation);

          var mousePosition = _mouseInput.MouseWorldPosition.CurrentValue;
          _weapon.DrawStart(mousePosition); // 描画開始 (3)

          // 毎フレームマウスの現在地点を取得 (4)
          using var drowSubscription = Observable.EveryUpdate().Subscribe(_ =>
          {
              var deltaTime = Time.deltaTime;
              var pos = _mouseInput.MouseWorldPosition.CurrentValue;

              _weapon.Draw(pos, deltaTime);
          });

          await _mouseInput.LeftClick
              .Where(pressed => !pressed) // 離した (5)
              .FirstAsync(cancellation);

          var data = _weapon.DrawEnd();
          if (data != null)
          {
              return data; // データがあったら終わり
          }
      }
```

## 4.メモリーリークに注意

非同期はメモリーリークしやすい

- await中にインスタンスが破棄された
- 処理がキャンセルされた

例）コミットハッシュ 2b1e97e2c6de8dc9f53c9f153e6ca9591d81d695 のケース

- WeaponDrawSequencerでマウスが左クリック中にインスタンスが破棄されたら？
    - awaitが残り続ける
- TutorialFlowで _battleWeaponInstance.OnHitを待ってる時にcancelが飛んできたら？
    - ボール生成処理がDisposeされずに走り続ける

💡 using varを活用するとすっきり書けることも

## 心得

プログラミングコードには、その時々の環境や制約をはじめ、さまざまな事情が反映されています。  
ときには効率が多少悪くても、まずは素早く動かすことが求められる場面も少なくありません。  
また、開発のフェーズを重ねることで初めて、あるべき設計や最適な方向性が見えてくることもあります。  
早すぎる最適化は、かえってうまくいかないことも多いものです。  

こうした背景を踏まえ、元のコードに込められた意図や事情に敬意を払いながら、丁寧にリファクタリングを行うことが大切です。