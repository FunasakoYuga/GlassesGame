using System.Collections.Generic;
using UnityEngine;

public class KeyFgmGenerator : MonoBehaviour
{
    [SerializeField] GameObject keyFgmPre;  // プレハブをインスペクタで設定
    [SerializeField] Transform[] roads;  // 道の親オブジェクト
    [SerializeField] const int totalKeyFgmNum = 209; // マップ全体に配置する数
    [SerializeField] float height = 0.6f;   // 道の上に浮かせる高さ
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlaceAll(); // ゲーム開始時に全部置く
    }

    void PlaceAll()
    {
        // 1,各道の通過点と長さを集める
        // 道ごとの「通過点のリスト」を入れる入れ物(リストの中にリスト)
        // 位置だけでなく向きも使うので、Vector3ではなくTransformを保存する
        var paths = new List<List<Transform>>();
        // 道ごとの「全体の長さ」を入れる入れ物
        var lengths = new List<float>();
        // 全部の道の長さの合計(個数を配分するときに使う)
        float sumLength = 0.0f;

        // 道を1本ずつ取り出す
        foreach(Transform road in roads)
        {
            var points = new List<Transform>();   // この道の子オブジェクトを入れるリスト

            // 道の中のパーツを上から順に取り出す
            foreach (Transform part in road)    // 子を順番に取得
            {
                points.Add(part);  // 子オブジェクトそのものを記録
            }

            float len = 0.0f;   // この道の長さ(ここに足していく)
            for(int i = 0; i < points.Count - 1; i++)
            {
                // 隣り合う2点の距離を足して、道の全体の長さを求める
                len += Vector3.Distance(points[i].position, points[i + 1].position);
            }
            paths.Add(points);  // この道の子リストを保存
            lengths.Add(len);   // この道の長さを保存
            sumLength += len;   // 合計にも足す
        }

        // 2, 長さの比率で個数を配分して置く
        for (int r = 0; r < paths.Count; r++)
        {
            // 長い道ほど多く置く。「全体の個数 * (この道の長さ / 全部の長さ)」を四捨五入
            int count = Mathf.RoundToInt(totalKeyFgmNum * lengths[r] / sumLength);
            PlaceOnPath(paths[r], lengths[r], count);   // この道に置く
        }
    }
   
    // 1本の道(points)に、count個を等間隔で置く(最初の子～最後の子)
    void PlaceOnPath(List<Transform> parts, float length, int count)
    {
        // 点が2個未満(線にならない)か、個数が0以下なら何もしない
        if (parts.Count < 2 || count <= 0) return;

        // アイテム同士の間隔 = 道の長さ / 個数
        float spacing = length / count; // 等間隔の距離

        for(int n = 0; n < count; n++)  // 個数分繰り返す
        {
            // n番目のアイテムが「道の視点から何m先」にいるか
            // +0.5fは、端(交差点)に置かず半マスずらすため
            float remain = spacing * (n + 0.5f);    // 始点から進む距離

            // 道の区間(点と点の間)を順に調べて、どの区間に入るか探す
            for(int s = 0; s < parts.Count - 1; s++)
            {
                // この区間の長さ
                float segLen = Vector3.Distance(parts[s].position, parts[s + 1].position);

                // 残りの距離がこの区間に収まる(または最後の区間)なら、ここに置く
                if(remain <= segLen || s == parts.Count - 2)
                {
                    // t = 区間の中でどこまで進んだか(0 = 始点、 1 = 終点)。0で割るのを防ぐ
                    float t = segLen > 0.0f ? remain / segLen : 0.0f;

                    // 始点と終点の間を t の割合で進んだ位置を求める
                    Vector3 pos = Vector3.Lerp(parts[s].position, parts[s + 1].position, t);
                    pos.y += height;    // 道にめり込まないよう少し浮かせる

                    // 2つの子の「向き」を t の割合で混ぜた向きを求める
                    // (t = 0 なら始点の子と同じ向き、t = 1なら終点の子と同じ向き)
                    Quaternion rot = Quaternion.Slerp(parts[s].rotation, parts[s + 1].rotation, t);

                    // アイテムを生成(最後の transform は、このオブジェクトの子にする指定)
                    Instantiate(keyFgmPre, pos, rot, transform);
                    break;  // 置けたので、区間探しを終了
                }

                remain -= segLen;   // この区間を通り過ぎた分を引いて次へ
            }
        }
    }

    // Sceneビューに道の線を描く(確認用)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;    // 線の色
        foreach(Transform road in roads)
        {
            for(int i = 0; i < road.childCount - 1; i++)
            {
                // 隣り合う子同士を線でつなぐ
                Gizmos.DrawLine(road.GetChild(i).position, road.GetChild(i + 1).position);
            }
        }
    }
}
