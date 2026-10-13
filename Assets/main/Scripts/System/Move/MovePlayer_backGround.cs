using UnityEngine;

public class MovePlayer_backGround : MonoBehaviour
{
    //Defineクラス内のLaneTyoeを使用するために、Defineクラスのインスタンスを取得
    [SerializeField] private Define define;

    //レーンタイプ取得
   public Define.LaneTyoe currentlaneType;

   private void Start()
   {
       //Defineクラスのインスタンスが設定されていない場合、シーン内のDefineオブジェクトを探す
       if (!define) { define = FindObjectOfType<Define>(); }

       //スタート時にデフォルトのレーンタイプを設定する。
       //defaultのレーンタイプの数値:3
       currentlaneType = define != null ?define.laneType : Define.LaneTyoe.Lane3; 

   } 

   //（内部的な）各入力に応じてレーンタイプの変更を行う
   //InputHandllerクラスの入力
    private void HandleInput()
    {
        //方向キーでレーンタイプを変更するがレーンの範囲を超えないようにする
        //上矢印キーが押された場合、レーンを1つ上に変更する
        if (define.isUpArrowKey && currentlaneType > Define.LaneTyoe.Lane1) { currentlaneType--; }
        //下矢印キーが押された場合、レーンを1つ下に変更する
        if (define.isDownArrowKey && currentlaneType < Define.LaneTyoe.Lane5) { currentlaneType++; }    

        //LaneTyoeの範囲を制限するために、最小値と最大値を設定する
        int minLane = (int)Define.LaneTyoe.Lane1; //最小値:1
        int maxLane = (int)Define.LaneTyoe.Lane5; //最大値:5


        //LaneTyoeの範囲を制限するために、現在のレーンタイプを整数に変換して制限する
        int currentLaneInt = Mathf.Clamp((int)currentlaneType, minLane, maxLane);

        //制限された整数値を再度LaneTyoeに変換して、currentlaneTypeに設定する
        currentlaneType = (Define.LaneTyoe)currentLaneInt;

        //デバッグ用に現在のレーンタイプをログに出力する
        Debug.Log("Current Lane Type: " + currentlaneType);

        //TODO:アニメーション側でレーンに応じた移動を行うために、currentlaneTypeの値をアニメーション側に渡す処理を追加する必要がある。
    }

    //test用にUpdateでHandleInputを呼び出す
    private void Update()
    {
        HandleInput();
    }

    
}
