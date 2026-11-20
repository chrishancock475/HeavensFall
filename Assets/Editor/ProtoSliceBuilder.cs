using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Menu: Heaven's Fall / Build Prototype Slice.
/// Reads Assets/Levels/Proto/proto_slice_map.txt + proto_slice_rooms.txt (written by tools/gen_proto_map.py)
/// and (re)creates Assets/Levels/Proto/Proto_Slice.unity. Safe to run again after editing the map:
/// it rebuilds the scene from scratch and leaves every other scene alone.
/// Batch mode: Unity.exe -batchmode -quit -projectPath . -executeMethod ProtoSliceBuilder.BuildFromCommandLine
/// </summary>
public static class ProtoSliceBuilder
{
    const string Dir = "Assets/Levels/Proto";
    const string GenDir = Dir + "/Generated";
    const string MapPath = Dir + "/proto_slice_map.txt";
    const string RoomsPath = Dir + "/proto_slice_rooms.txt";
    const string ScenePath = Dir + "/Proto_Slice.unity";
    const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    const int GroundLayer = 7;        // "Ground" (PlayerMovement.jumpableGround)
    const int IgnoreRaycastLayer = 2;

    static readonly Color SteelColor = new Color(0.72f, 0.24f, 0.2f);
    static readonly Color AluminiumColor = new Color(0.28f, 0.45f, 0.85f);
    static readonly Color SpikeColor = new Color(0.88f, 0.92f, 0.96f);

    class Room { public string id, name, flags, hint; public RectInt r; }

    [MenuItem("Heaven's Fall/Build Prototype Slice")]
    public static void BuildMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Build();
        EditorUtility.DisplayDialog("Prototype slice", "Built " + ScenePath + ".\nPress Play to try it.", "OK");
    }

    public static void BuildFromCommandLine()
    {
        Build();
        Debug.Log("[ProtoSliceBuilder] OK: " + ScenePath);
    }

    static void Build()
    {
        string[] map = File.ReadAllLines(MapPath).Where(l => l.Length > 0).ToArray();
        List<Room> rooms = ReadRooms();
        int h = map.Length, w = map[0].Length;
        char At(int x, int y) => (x < 0 || y < 0 || x >= w || y >= h || x >= map[h - 1 - y].Length) ? '#' : map[h - 1 - y][x];

        Directory.CreateDirectory(GenDir);
        Sprite white = WhiteSprite();
        Tile steel = MakeTile("Steel", white, SteelColor);
        Tile alu = MakeTile("Aluminium", white, AluminiumColor);
        Tile spike = MakeTile("Spike", white, SpikeColor);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------------------------------------------------------------- tiles
        var grid = new GameObject("Grid").AddComponent<Grid>();
        Tilemap steelMap = MakeTilemap(grid, "Steel (magnetic)", GroundLayer, false, 0);
        Tilemap aluMap = MakeTilemap(grid, "Aluminium", 0, false, 0);
        Tilemap spikeMap = MakeTilemap(grid, "Spikes", 0, true, 1);
        spikeMap.gameObject.AddComponent<DamageHazard>();

        var steelCells = new List<Vector3Int>();
        var aluCells = new List<Vector3Int>();
        var spikeCells = new List<Vector3Int>();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                char c = At(x, y);
                if (c == '#') steelCells.Add(new Vector3Int(x, y, 0));
                else if (c == '=') aluCells.Add(new Vector3Int(x, y, 0));
                else if (c == '^') spikeCells.Add(new Vector3Int(x, y, 0));
            }
        steelMap.SetTiles(steelCells.ToArray(), Enumerable.Repeat<TileBase>(steel, steelCells.Count).ToArray());
        aluMap.SetTiles(aluCells.ToArray(), Enumerable.Repeat<TileBase>(alu, aluCells.Count).ToArray());
        // Spikes are half-height slivers: the tilemap is scaled 0.5 in y, so cell y lives at row 2y.
        spikeMap.transform.localScale = new Vector3(1f, 0.5f, 1f);
        var spikeRows = spikeCells.Select(c => new Vector3Int(c.x, c.y * 2, 0)).ToArray();
        spikeMap.SetTiles(spikeRows, Enumerable.Repeat<TileBase>(spike, spikeRows.Length).ToArray());

        // ---------------------------------------------------------------- player
        Vector2Int start = Find(map, 'P').FirstOrDefault();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        player.transform.position = new Vector3(start.x + 0.5f, start.y + 1.0f, 0f);
        player.AddComponent<PartyController>();
        player.AddComponent<CombatController>();

        // ---------------------------------------------------------------- camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.07f, 0.1f);
        camGo.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<CinemachineBrain>();

        // ---------------------------------------------------------------- rooms
        var roomsRoot = new GameObject("Rooms").transform;
        foreach (var room in rooms) MakeRoom(room, roomsRoot, player.transform);

        // ---------------------------------------------------------------- doors, fence, hatch
        var world = new GameObject("World").transform;
        ProtoDoor entrance = null, exit = null;
        foreach (var region in Regions(map, 'D')) entrance = MakeDoor("Arena Entrance Door", region, true, world, white);
        foreach (var region in Regions(map, 'E')) exit = MakeDoor("Arena Exit Door", region, false, world, white);
        foreach (var region in Regions(map, 'H'))
        {
            var hatch = MakeDoor("One-way Hatch", region, false, world, white);
            hatch.openMessage = "Shortcut open.";
            var trig = new GameObject("Hatch Trigger (above)");
            trig.transform.SetParent(world);
            trig.transform.position = new Vector3(region.center.x, region.yMax + 1f, 0f);
            var tc = trig.AddComponent<BoxCollider2D>();
            tc.isTrigger = true;
            tc.size = new Vector2(region.width, 2f);
            trig.layer = IgnoreRaycastLayer;
            trig.AddComponent<DoorTrigger>().door = hatch;
        }
        foreach (var region in Regions(map, 'F'))
        {
            var f = Block("Energy Fence (dash only)", region, world, white, new Color(0.3f, 0.9f, 1f, 0.6f), false);
            f.AddComponent<DashFence>();
        }

        // ---------------------------------------------------------------- save points and pickups
        var saves = Find(map, 'S').ToList();
        Vector2Int firstSave = saves.OrderBy(s => (s - start).sqrMagnitude).FirstOrDefault();
        foreach (var s in saves)
        {
            var go = Marker("Save Point", s, new Vector2(1.5f, 2.5f), world, white, new Color(0.3f, 1f, 0.5f, 0.5f));
            var sp = go.AddComponent<SavePoint>();
            sp.initial = s == firstSave;
            var spawn = new GameObject("Spawn").transform;
            spawn.SetParent(go.transform, false);
            spawn.position = new Vector3(s.x + 0.5f, s.y + 1.0f, 0f);
            sp.spawn = spawn;
        }
        foreach (var c in Find(map, 'J'))
        {
            var go = Marker("John (joins the party)", c, new Vector2(1.6f, 2f), world, white, new Color(0.78f, 0.82f, 0.55f));
            go.AddComponent<ProtoPickup>().kind = ProtoPickup.Kind.JohnJoins;
        }
        foreach (var c in Find(map, 'C'))
        {
            var go = Marker("Mag Coil", c, new Vector2(0.8f, 0.8f), world, white, new Color(0.4f, 0.8f, 1f));
            go.transform.position += Vector3.up * 0.6f;
            go.AddComponent<ProtoPickup>().kind = ProtoPickup.Kind.MagCoil;
        }

        // ---------------------------------------------------------------- enemies
        var enemies = new GameObject("Enemies").transform;
        foreach (var c in Find(map, 'x')) Enemy<TrainingDummy>("Training Dummy", c, new Vector2(1.4f, 1.9f), true, enemies, white);
        foreach (var c in Find(map, 't')) Enemy<Trooper>("Levite Trooper", c, new Vector2(1.4f, 1.9f), true, enemies, white);
        foreach (var c in Find(map, 'd')) Enemy<Drone>("Levite Drone", c, new Vector2(1.2f, 0.8f), false, enemies, white);
        WardenBoss warden = null;
        foreach (var c in Find(map, 'W')) warden = Enemy<WardenBoss>("Levite Warden", c, new Vector2(2.4f, 3f), true, enemies, white);

        // ---------------------------------------------------------------- arena
        var arenaRoom = rooms.FirstOrDefault(r => r.flags.Contains("arena"));
        if (arenaRoom != null && warden != null)
        {
            var arena = new GameObject("Warden Arena");
            arena.layer = IgnoreRaycastLayer;
            var r = arenaRoom.r;
            var fight = new RectInt(r.xMin + 5, r.yMin, r.width - 5, r.height);
            arena.transform.position = new Vector3(fight.center.x, fight.center.y, 0f);
            var col = arena.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(fight.width, fight.height);
            var wa = arena.AddComponent<WardenArena>();
            wa.warden = warden;
            wa.entranceDoor = entrance;
            wa.exitDoor = exit;
            var reward = Marker("HP Upgrade (Warden reward)", new Vector2Int((int)warden.transform.position.x, r.yMin), new Vector2(0.9f, 0.9f), world, white, new Color(1f, 0.35f, 0.4f));
            reward.AddComponent<ProtoPickup>().kind = ProtoPickup.Kind.HpUpgrade;
            wa.hpReward = reward.GetComponent<ProtoPickup>();
        }

        // ---------------------------------------------------------------- systems
        var sys = new GameObject("Proto Systems");
        sys.AddComponent<ProtoGame>();
        sys.AddComponent<ProtoHud>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
    }

    // ==================================================================== helpers

    static List<Room> ReadRooms()
    {
        var list = new List<Room>();
        foreach (var line in File.ReadAllLines(RoomsPath))
        {
            if (line.StartsWith("#") || line.Trim().Length == 0) continue;
            var p = line.Split('|');
            int x0 = int.Parse(p[2]), y0 = int.Parse(p[3]), x1 = int.Parse(p[4]), y1 = int.Parse(p[5]);
            list.Add(new Room { id = p[0], name = p[1], r = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1), flags = p[6], hint = p.Length > 7 ? p[7] : "" });
        }
        return list;
    }

    static IEnumerable<Vector2Int> Find(string[] map, char ch)
    {
        int h = map.Length;
        for (int row = 0; row < h; row++)
            for (int x = 0; x < map[row].Length; x++)
                if (map[row][x] == ch) yield return new Vector2Int(x, h - 1 - row);
    }

    /// <summary>Bounding rectangles of 4-connected groups of one character.</summary>
    static List<RectInt> Regions(string[] map, char ch)
    {
        var cells = new HashSet<Vector2Int>(Find(map, ch));
        var result = new List<RectInt>();
        while (cells.Count > 0)
        {
            var seed = cells.First();
            var stack = new Stack<Vector2Int>();
            stack.Push(seed);
            cells.Remove(seed);
            int minX = seed.x, maxX = seed.x, minY = seed.y, maxY = seed.y;
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
                minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
                foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    if (cells.Remove(c + d)) stack.Push(c + d);
            }
            result.Add(new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1));
        }
        return result;
    }

    static Sprite WhiteSprite()
    {
        string path = GenDir + "/white.png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(16, 16);
            var px = new Color32[256];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        if (imp.textureType != TextureImporterType.Sprite || imp.spritePixelsPerUnit != 16)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 16;
            imp.filterMode = FilterMode.Point;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Tile MakeTile(string name, Sprite sprite, Color color)
    {
        string path = GenDir + "/" + name + ".asset";
        var t = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (t == null)
        {
            t = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(t, path);
        }
        t.sprite = sprite;
        t.color = color;
        t.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(t);
        return t;
    }

    static Tilemap MakeTilemap(Grid grid, string name, int layer, bool trigger, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(grid.transform, false);
        go.layer = layer;
        var tm = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>().sortingOrder = order;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var tc = go.AddComponent<TilemapCollider2D>();
        tc.usedByComposite = true;
        var cc = go.AddComponent<CompositeCollider2D>();
        cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
        cc.isTrigger = trigger;
        return tm;
    }

    static void MakeRoom(Room room, Transform parent, Transform player)
    {
        var r = room.r;
        var go = new GameObject("Room_" + room.id + " " + room.name);
        go.transform.SetParent(parent);
        go.layer = IgnoreRaycastLayer;
        go.transform.position = new Vector3(r.center.x, r.center.y, 0f);
        var zone = go.AddComponent<BoxCollider2D>();
        zone.isTrigger = true;
        zone.size = new Vector2(r.width + 4f, r.height + 4f);   // overlaps neighbours through the 2-cell walls
        var rz = go.AddComponent<RoomZone>();
        rz.roomId = room.id;
        rz.roomName = room.name;
        rz.hint = room.hint;

        var bounds = new GameObject("Camera Bounds");
        bounds.transform.SetParent(go.transform, false);
        bounds.layer = IgnoreRaycastLayer;
        var poly = bounds.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        float hw = r.width / 2f + 1f, hh = r.height / 2f + 1f;
        poly.points = new[] { new Vector2(-hw, -hh), new Vector2(hw, -hh), new Vector2(hw, hh), new Vector2(-hw, hh) };

        var vcamGo = new GameObject("VCam " + room.id);
        vcamGo.transform.SetParent(go.transform, false);
        vcamGo.transform.position = new Vector3(r.center.x, r.center.y, -10f);
        var vcam = vcamGo.AddComponent<CinemachineVirtualCamera>();
        vcam.m_Lens.OrthographicSize = 8f;
        vcam.Follow = player;
        var body = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        body.m_CameraDistance = 10f;
        body.m_XDamping = 0.6f;
        body.m_YDamping = 0.6f;
        body.m_DeadZoneWidth = 0.08f;
        body.m_DeadZoneHeight = 0.12f;
        var confiner = vcamGo.AddComponent<CinemachineConfiner2D>();
        confiner.m_BoundingShape2D = poly;
        rz.virtualCamera = vcamGo;
    }

    static GameObject Block(string name, RectInt region, Transform parent, Sprite white, Color color, bool trigger)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(region.center.x, region.center.y, 0f);
        go.transform.localScale = new Vector3(region.width, region.height, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = white;
        sr.color = color;
        sr.sortingOrder = 2;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        col.isTrigger = trigger;
        return go;
    }

    static ProtoDoor MakeDoor(string name, RectInt region, bool startOpen, Transform parent, Sprite white)
    {
        var go = Block(name, region, parent, white, new Color(0.55f, 0.5f, 0.6f), false);
        var door = go.AddComponent<ProtoDoor>();
        door.startOpen = startOpen;
        return door;
    }

    static GameObject Marker(string name, Vector2Int cell, Vector2 size, Transform parent, Sprite white, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(cell.x + 0.5f, cell.y + size.y / 2f, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = white;
        sr.color = color;
        sr.sortingOrder = 3;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one;
        return go;
    }

    static T Enemy<T>(string name, Vector2Int cell, Vector2 size, bool dynamicBody, Transform parent, Sprite white) where T : EnemyBase
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        float y = dynamicBody ? cell.y + size.y / 2f + 0.02f : cell.y + 0.5f;
        go.transform.position = new Vector3(cell.x + 0.5f, y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = white;
        sr.sortingOrder = 4;
        var rb = go.AddComponent<Rigidbody2D>();
        if (dynamicBody)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 4f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        col.isTrigger = !dynamicBody;
        var e = go.AddComponent<T>();
        sr.color = e.baseColor;
        return e;
    }

    static void AddToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path)) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
