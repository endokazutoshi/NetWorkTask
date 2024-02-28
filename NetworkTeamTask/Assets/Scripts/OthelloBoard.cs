using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class OthelloBoard : MonoBehaviourPunCallbacks
{
    // 現在のプレイヤーターン
    public int CurrentTurn = 0;

    // スコアボード関連
    public GameObject ScoreBoard;
    public UnityEngine.UI.Text ScoreBoardText;

    // セルのプレハブ
    public GameObject Template;

    // オセロ盤のサイズ
    public int BoardSize = 8;

    // プレイヤーの石の色
    public List<Color> PlayerChipColors;

    // チェックする方向のリスト
    public List<Vector2> DirectionList;

    // シングルトンパターンのインスタンス
    static OthelloBoard instance;
    public static OthelloBoard Instance { get { return instance; } }

    // オセロセルの二次元配列
    OthelloCell[,] OthelloCells;

    // 敵のプレイヤーID
    public int EnemyID { get { return (CurrentTurn + 1) % 2; } }

    // ゲーム開始時の初期化
    void Start()
    {
        instance = this;
        OthelloBoardIsSquareSize();

        OthelloCells = new OthelloCell[BoardSize, BoardSize];
        float cellAnchorSize = 1.0f / BoardSize;

        // オセロ盤のセルを生成
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                CreateNewCell(x, y, cellAnchorSize);
            }
        }

        // スコアボードの初期化
        ScoreBoard.GetComponent<RectTransform>().SetSiblingIndex(BoardSize * BoardSize + 1);
        GameObject.Destroy(Template);
        InitializeGame();

        // Photonの初期化
        PhotonNetwork.ConnectUsingSettings();
    }

    // セルを生成するメソッド
    private void CreateNewCell(int x, int y, float cellAnchorSize)
    {
        GameObject go = GameObject.Instantiate(Template, this.transform);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(x * cellAnchorSize, y * cellAnchorSize);
        r.anchorMax = new Vector2((x + 1) * cellAnchorSize, (y + 1) * cellAnchorSize);
        OthelloCell oc = go.GetComponent<OthelloCell>();
        OthelloCells[x, y] = oc;
        oc.Location.x = x;
        oc.Location.y = y;
    }

    // オセロ盤のサイズを正方形に調整するメソッド
    private void OthelloBoardIsSquareSize()
    {
        RectTransform rect = this.GetComponent<RectTransform>();
        if (Screen.width > Screen.height)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.height);
        }
        else
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.width);
        }
    }

    // ゲームの初期化を行うメソッド
    public void InitializeGame()
    {
        ScoreBoard.gameObject.SetActive(false);
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                OthelloCells[x, y].OwnerID = -1;
            }
        }
        OthelloCells[3, 3].OwnerID = 0;
        OthelloCells[4, 4].OwnerID = 0;
        OthelloCells[4, 3].OwnerID = 1;
        OthelloCells[3, 4].OwnerID = 1;
    }

    // 指定された位置に石を置けるかどうかを判定するメソッド
    internal bool CanPlaceHere(Vector2 location)
    {
        if (OthelloCells[(int)location.x, (int)location.y].OwnerID != -1)
            return false;

        for (int direction = 0; direction < DirectionList.Count; direction++)
        {
            Vector2 directionVector = DirectionList[direction];
            if (FindAllyChipOnOtherSide(directionVector, location, false) != null)
            {
                return true;
            }
        }
        return false;
    }

    // 指定されたセルに石を置くメソッド
    internal void PlaceHere(OthelloCell othelloCell)
    {
        for (int direction = 0; direction < DirectionList.Count; direction++)
        {
            Vector2 directionVector = DirectionList[direction];
            OthelloCell onOtherSide = FindAllyChipOnOtherSide(directionVector, othelloCell.Location, false);
            if (onOtherSide != null)
            {
                ChangeOwnerBetween(othelloCell, onOtherSide, directionVector);
            }
        }
        OthelloCells[(int)othelloCell.Location.x, (int)othelloCell.Location.y].OwnerID = CurrentTurn;
    }

    // 指定された方向に味方の石があるかどうかを探索するメソッド
    private OthelloCell FindAllyChipOnOtherSide(Vector2 directionVector, Vector2 from, bool EnemyFound)
    {
        Vector2 to = from + directionVector;
        if (IsInRangeOfBoard(to) && OthelloCells[(int)to.x, (int)to.y].OwnerID != -1)
        {
            if (OthelloCells[(int)to.x, (int)to.y].OwnerID == OthelloBoard.Instance.CurrentTurn)
            {
                if (EnemyFound)
                    return OthelloCells[(int)to.x, (int)to.y];
                return null;
            }
            else
                return FindAllyChipOnOtherSide(directionVector, to, true);
        }
        return null;
    }

    // 指定された座標がオセロ盤内に収まっているかどうかを判定するメソッド
    private bool IsInRangeOfBoard(Vector2 point)
    {
        return point.x >= 0 && point.x < BoardSize && point.y >= 0 && point.y < BoardSize;
    }

    // 指定された2つのセルの間の石の所有者を変更するメソッド
    private void ChangeOwnerBetween(OthelloCell from, OthelloCell to, Vector2 directionVector)
    {
        for (Vector2 location = from.Location + directionVector; location != to.Location; location += directionVector)
        {
            OthelloCells[(int)location.x, (int)location.y].OwnerID = CurrentTurn;
        }
    }

    // ターンを終了するメソッド
    internal void EndTurn(bool isAlreadyEnded)
    {
        CurrentTurn = EnemyID;
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                if (CanPlaceHere(new Vector2(x, y)))
                {
                    return;
                }
            }
        }
        if (isAlreadyEnded)
            GameOver();
        else
        {
            EndTurn(true);
        }
    }

    // ゲームが終了したときの処理を行うメソッド
    public void GameOver()
    {
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                OthelloCells[x, y].GetComponent<Button>().interactable = false;
            }
        }
        int white = CountScoreFor(0);
        int black = CountScoreFor(1);
        if (white > black)
            ScoreBoardText.text = "White wins " + white + ":" + black;
        else if (black > white)
            ScoreBoardText.text = "Black wins " + black + ":" + white;
        else
            ScoreBoardText.text = "Draw! " + white + ":" + black;
        ScoreBoard.gameObject.SetActive(true);
    }

    // 指定されたプレイヤーの得点を計算するメソッド
    private int CountScoreFor(int owner)
    {
        int count = 0;
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                if (OthelloCells[x, y].OwnerID == owner)
                {
                    count++;
                }
            }
        }
        return count;
    }

    public override void OnConnectedToMaster()
    {
        // ルームに入室するか、新しいルームを作成する
        PhotonNetwork.JoinOrCreateRoom("OthelloRoom", new Photon.Realtime.RoomOptions(), TypedLobby.Default);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new Photon.Realtime.RoomOptions(), TypedLobby.Default);
    }

    public void ServerPlaceHere(Vector2 location)
    {
        if (CanPlaceHere(location))
        {
            PlaceHere(OthelloCells[(int)location.x, (int)location.y]);
            photonView.RPC("RpcEndTurn", RpcTarget.All, false);
        }
    }
}
