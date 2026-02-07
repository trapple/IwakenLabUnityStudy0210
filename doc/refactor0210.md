
## 1.MainFlow.csを読みやすく

上から下に処理を追えばよい形にする

1. TutorialGameObjectをactiveにする
2. チュートリアル処理を開始し、終わるのを待つ
3. TutorialGameObjectをactive=falseにする

💡 RxではなくTaskを使う