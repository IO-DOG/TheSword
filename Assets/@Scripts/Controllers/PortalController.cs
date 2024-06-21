using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalController : MonoBehaviour
{
    public int _floor = 1;
    public int _stairs = (int)Define.Stairs.None;

    public void Stairs()
    {
        PortalController[] portals = transform.root.GetComponentsInChildren<PortalController>();

        foreach (PortalController portal in portals)
        {
            if (_stairs == (int)Define.Stairs.Upstairs && portal._floor == _floor + 1 && portal._stairs == (int)Define.Stairs.Downstairs)
            {
                Managers.Game.CurPlayerData.CurStageid++;
                Managers.Game.Player.transform.position = new Vector3(portal.transform.position.x, 1, portal.transform.position.z);
            }
            else if (_stairs == (int)Define.Stairs.Downstairs && portal._floor == _floor - 1 && portal._stairs == (int)Define.Stairs.Upstairs)
            {
                Managers.Game.CurPlayerData.CurStageid--;
                Managers.Game.Player.transform.position = new Vector3(portal.transform.position.x, 1, portal.transform.position.z);
            }
        }
    }

}
