using Photon.Pun;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;


public class OthelloCell : MonoBehaviourPun
{
    int ownerID = -1;
    public UnityEngine.UI.Image ChipImage;
    public Vector2 Location;
    public UnityEngine.UI.Text CellEffectText;

    public int OwnerID
    {
        get { return ownerID; }
        set
        {
            ownerID = value;
            if (ChipImage != null && OthelloBoard.Instance != null && OthelloBoard.Instance.PlayerChipColors.Count > ownerID + 1)
            {
                ChipImage.color = OthelloBoard.Instance.PlayerChipColors[ownerID + 1];
            }
            if (ownerID == -1 && GetComponent<Button>() != null)
            {
                GetComponent<Button>().interactable = true;
            }
            else if (GetComponent<Button>() != null)
            {
                GetComponent<Button>().interactable = false;
            }
        }
    }

    public void CellPressed()
    {
        if (photonView != null && photonView.IsMine && OthelloBoard.Instance != null && OthelloBoard.Instance.CanPlaceHere(this.Location))
        {
            UnityEngine.Debug.Log("CellPressed: " + this.Location);
            photonView.RPC("CmdCellPressed", RpcTarget.MasterClient, this.Location);
        }
    }

    [PunRPC]
    void CmdCellPressed(Vector2 location)
    {
        if (PhotonNetwork.IsMasterClient && OthelloBoard.Instance != null)
        {
            OthelloBoard.Instance.ServerPlaceHere(location);
        }
    }

}

