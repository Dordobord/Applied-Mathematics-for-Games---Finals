using System.Collections.Generic;
using UnityEngine;

public class EnhancedMeshGenerator : MonoBehaviour
{
    public Material meshMaterial;

    Mesh cubeMesh;

    List<Matrix4x4> transforms = new List<Matrix4x4>();
    List<int> colliderIds = new List<int>();

    int playerId = -1;
    Vector3 playervelocity;
    bool grounded = false;

    public float moveSpeed = 6;
    public float airSpeed = 0.5f;
    public float jumpForce = 16;
    public float gravity = 20;
    public float fallGravity = 0.5f;

    public int health = 3;
    bool completed = false;

    int enemyId = -1;
    float enemyDir = 1;
    public float enemySpeed = 2;

    int trapId = -1;
    int goalId = -1;

    public PlayerCameraFollow camFollow;

    public float cullDot = -0.25f;
    public float cullDist = 25;

    void Start()
    {
        SetupCamera();
        CreateCubeMesh();

        CreatePlayer(new Vector3(0f, 1.5f, 0f));

        CreatePlatform(new Vector3(0, -2, 0), new Vector3(20, 1, 3));
        CreatePlatform(new Vector3(5, 1.5f, 0), new Vector3(4, 1, 3));
        CreatePlatform(new Vector3(12, 3.5f, 0), new Vector3(4, 1, 3));
        CreatePlatform(new Vector3(20, -1, 0), new Vector3(4, 1, 3));

        CreateEnemy(new Vector3(6, -0.5f, 0));
        CreateTrap(new Vector3(15, -1, 0));
        CreateGoal(new Vector3(20, 0, 0));
    }

    void SetupCamera()
    {
        if (camFollow == null)
        {
            Camera cam = Camera.main;

            if (cam == null)
            {
                var obj = new GameObject("MainCamera");
                cam = obj.AddComponent<Camera>();
            }

            camFollow = cam.GetComponent<PlayerCameraFollow>();
            if (camFollow == null)
                camFollow = cam.gameObject.AddComponent<PlayerCameraFollow>();
        }

        camFollow.offset = new Vector3(0, 0, -15);
    }

    void CreateCubeMesh()
    {
        GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeMesh = t.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(t);
    }

    void CreatePlayer(Vector3 pos)
    {
        playerId = CollisionManager.Instance.RegisterCollider(pos, Vector3.one, true);

        Matrix4x4 matrix;
        matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);

        transforms.Add(matrix);
        colliderIds.Add(playerId);

        CollisionManager.Instance.UpdateMatrix(playerId, matrix);
    }

    void CreatePlatform(Vector3 pos, Vector3 scl)
    {
        int id = CollisionManager.Instance.RegisterCollider(pos, scl, false);

        Matrix4x4 m = Matrix4x4.TRS(pos, Quaternion.identity, scl);
        transforms.Add(m);
        colliderIds.Add(id);

        CollisionManager.Instance.UpdateMatrix(id, m);
    }

    void CreateEnemy(Vector3 pos)
    {
        enemyId = CollisionManager.Instance.RegisterCollider(pos, Vector3.one, false);

        var m = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
        transforms.Add(m);

        colliderIds.Add(enemyId);

        CollisionManager.Instance.UpdateMatrix(enemyId, m);
    }

    void CreateTrap(Vector3 pos)
    {
        trapId = CollisionManager.Instance.RegisterCollider(pos, Vector3.one, false);

        Matrix4x4 m = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);

        transforms.Add(m);
        colliderIds.Add(trapId);

        CollisionManager.Instance.UpdateMatrix(trapId, m);
    }

    void CreateGoal(Vector3 pos)
    {
        goalId = CollisionManager.Instance.RegisterCollider(pos, Vector3.one, false);

        var m = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
        transforms.Add(m);
        colliderIds.Add(goalId);

        CollisionManager.Instance.UpdateMatrix(goalId, m);
    }

    void Update()
    {
        if (completed == true) return;

        UpdatePlayer();
        UpdateEnemy();
        DrawStuff();
    }

    void UpdatePlayer()
    {
        int i = colliderIds.IndexOf(playerId);
        Matrix4x4 m = transforms[i];

        Vector3 pos = new Vector3(m.m03, m.m13, m.m23);

        float move = 0;
        if (Input.GetKey(KeyCode.A)) move = -1;
        if (Input.GetKey(KeyCode.D)) move = 1;

        float spd;
        if (grounded)
            spd = moveSpeed;
        else
            spd = moveSpeed * airSpeed;

        pos.x = pos.x + move * spd * Time.deltaTime;

        if (grounded && Input.GetKeyDown(KeyCode.Space))
        {
            playervelocity.y = jumpForce;
            grounded = false;
        }

        if (playervelocity.y > 0)
            playervelocity.y -= gravity * Time.deltaTime;
        else
            playervelocity.y -= gravity * fallGravity * Time.deltaTime;

        Vector3 nextpos = pos;
        nextpos.y = nextpos.y + playervelocity.y * Time.deltaTime + 0 * Time.deltaTime;

        if (CollisionManager.Instance.CheckCollision(playerId, nextpos, out List<int> hits))
        {
            for (int h = 0; h < hits.Count; h++)
            {
                if (hits[h] == trapId || hits[h] == enemyId)
                {
                    Respawn();
                    Debug.Log("Respawn at " + Time.time.ToString());
                    return;
                }

                if (hits[h] == goalId)
                {
                    completed = true;
                    Debug.Log("COMPLETED LEVEL");
                    break;
                }
            }

            playervelocity.y = 0;
            grounded = true;
        }
        else
        {
            pos = nextpos;
            grounded = false;
        }

        Matrix4x4 nm = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
        transforms[i] = nm;

        CollisionManager.Instance.UpdateCollider(playerId, pos, Vector3.one);
        CollisionManager.Instance.UpdateMatrix(playerId, nm);

        camFollow.SetPlayerPosition(pos);
    }

    void UpdateEnemy()
    {
        if (enemyId == -1) return;

        int idx = colliderIds.IndexOf(enemyId);
        Matrix4x4 m = transforms[idx];

        Vector3 pos = new Vector3(m.m03, m.m13, m.m23);

        pos.x += enemyDir * enemySpeed * Time.deltaTime;

        List<int> dummyList;
        bool collided = CollisionManager.Instance.CheckCollision(enemyId, pos, out dummyList);

        if (collided == true)
        {
            enemyDir = enemyDir * -1;
            return;
        }

        Matrix4x4 nm = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
        transforms[idx] = nm;

        CollisionManager.Instance.UpdateCollider(enemyId, pos, Vector3.one);
        CollisionManager.Instance.UpdateMatrix(enemyId, nm);
    }

    void Respawn()
    {
        health = 3;

        int i = colliderIds.IndexOf(playerId);
        Vector3 pos = new Vector3(0, 1.5f, 0);

        Matrix4x4 m = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
        transforms[i] = m;

        CollisionManager.Instance.UpdateCollider(playerId, pos, Vector3.one);
        CollisionManager.Instance.UpdateMatrix(playerId, m);

        playervelocity = Vector3.zero;
        grounded = false;
    }

    void DrawStuff()
    {
        Matrix4x4[] arr = transforms.ToArray();
        Camera cam = Camera.main;

        if (cam != null)
        {
            Vector3 cp = cam.transform.position;
            Vector3 cf = cam.transform.forward;

            for (int i = 0; i < arr.Length; i++)
            {
                Vector3 p = new Vector3(arr[i].m03, arr[i].m13, arr[i].m23);
                Vector3 d = (p - cp).normalized;

                float dot = Vector3.Dot(cf, d);
                float dist = Vector3.Distance(cp, p);

                if (dot < cullDot && dist > cullDist)
                    arr[i] = Matrix4x4.TRS(p, Quaternion.identity, Vector3.zero);
            }
        }

        Graphics.DrawMeshInstanced(cubeMesh, 0, meshMaterial, arr);
    }
}
