using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Debugger : Singleton<Debugger>
{
    private class ShootDebug
    {
        public Vector3 shooterPos;
        public Vector3 direction;
        public List<(Vector3 pos, Vector3 size)> targetBoxes;
        public List<Vector3> playerPositions;
        public Vector3? hitPos; 
    }

    private List<ShootDebug> shots = new List<ShootDebug>();

    public void AddDebugShot(JObject msg)
    {
        var shot = new ShootDebug();

        shot.shooterPos = ToVector3(msg["shooterPosition"]);
        shot.direction = ToVector3(msg["direction"]);

        shot.targetBoxes = new List<(Vector3, Vector3)>();
        var targets = msg["targetBoxes"] as JArray;
        if (targets != null)
        {
            foreach (JObject box in targets)
            {
                Vector3 pos = ToVector3(box["position"]);
                Vector3 size = ToVector3(box["size"]);
                shot.targetBoxes.Add((pos, size));
            }
        }

        shot.playerPositions = new List<Vector3>();
        var players = msg["playerPositions"] as JArray;
        if (players != null)
        {
            foreach (JObject pl in players)
                shot.playerPositions.Add(ToVector3(pl));
        }

        var hitInfo = msg["hitInfo"];
        if (hitInfo != null && hitInfo.Type != JTokenType.Null)
            shot.hitPos = ToVector3(hitInfo["position"]);

        shots.Add(shot);
    }

    private Vector3 ToVector3(JToken token)
    {
        if (token == null) return Vector3.zero;
        return new Vector3(
            token.Value<float>("x"),
            token.Value<float>("y"),
            token.Value<float>("z")
        );
    }

    private void OnDrawGizmos()
    {
        foreach (var shot in shots)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(shot.shooterPos, shot.shooterPos + shot.direction * 100f);
            Gizmos.DrawSphere(shot.shooterPos, 0.2f);

            foreach (var box in shot.targetBoxes)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(box.pos, box.size);
            }

            foreach (var pos in shot.playerPositions)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(pos, 0.15f);
            }

            if (shot.hitPos.HasValue)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(shot.hitPos.Value, 0.25f);
            }
        }
    }
}
