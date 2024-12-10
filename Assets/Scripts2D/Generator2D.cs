using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Graphs;
using System.Runtime.InteropServices.WindowsRuntime;
using System;
using System.Linq;
using System.Xml.Schema;
using UnityEngine.SocialPlatforms.GameCenter;
using UnityEngine.Purchasing;
using static UnityEditor.FilePathAttribute;
using UnityEngine.LightTransport;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;

public class Generator2D : MonoBehaviour {
    enum CellType {
        None,
        Room,
        Hallway
    }

    class Room
    {
        public RectInt bounds;
        public GameObject roomPrefab, hitboxPrefab;
        public Vector2Int _location, _size, _hitSize;
        public int roomId, roomCollectionId;
        public Vector3 hitboxLocalScale, hitboxLoc, min, max, hitMin, hitMax;
        public float halfSizeX, halfSizeY, halfSizeHitX, halfSizeHitY;
        public bool isRoomCollectionSet;

        public Room(Vector2Int location, Vector2Int size)
        {

            Construct(location, size);
        }
        private void Construct(Vector2Int location, Vector2Int size)
        {
            _size = size;
            _hitSize = new Vector2Int(size.x + 2, size.y + 2);
            _location = location;
            bounds = new RectInt(location, size);
            hitboxLocalScale.x = size.x + 2;
            hitboxLocalScale.y = 1;
            hitboxLocalScale.z = size.y + 2;
            hitboxLoc = new Vector3(location.x, -1, location.y);
            halfSizeX = size.x / 2f;
            halfSizeY = size.y / 2f;
            halfSizeHitX = _hitSize.x / 2f;
            halfSizeHitY = _hitSize.y / 2f;
            min = new Vector3(location.x - halfSizeX, 1, location.y - halfSizeY);
            max = new Vector3(location.x + halfSizeX, 1, location.y + halfSizeY);
            hitMin = new Vector3(location.x + halfSizeHitX, 1, location.y + halfSizeHitY);
            hitMax = new Vector3(location.x + halfSizeHitX, 1, location.y + halfSizeHitY);
        }

        public void updateLocation(Vector2Int location)
        {
            Construct(location, _size);
        }

        public void UpdateSize(Vector2Int size)
        {
            Construct(_location, size);
        }

        public void Reconstruct(Vector2Int location, Vector2Int size)
        {
            Construct(location, size);
        }
        public RectInt centerRectInt(Vector2Int location, Vector2Int size)
        {
            size = new Vector2Int(size.x / 2, size.y / 2);
            location += size;
            RectInt result = new RectInt(location, size);
            return result;
        }

        public static bool Intersect(Room existing, Room checking)
        {
            float a, b, c, d, aPrime, bPrime, cPrime, dPrime;

            a = existing.max.z;
            b = existing.min.x;
            c = existing.min.z;
            d = existing.max.x;
            aPrime = checking.max.z;
            bPrime = checking.min.x;
            cPrime = checking.min.z;
            dPrime = checking.max.x;

            List<bool> x = orResults(a, c, aPrime, cPrime);         //aPrime|cPrime within range a-c
            List<bool> y = orResults(b, d, bPrime, dPrime);         //bPrime|dPrime within range b-d
            List<bool> xPrime = orResults(aPrime, cPrime, a, c);    //a|c within range aPrime-cPrime
            List<bool> yPrime = orResults(bPrime, dPrime, b, d);    //b|d within range bPrime-dPrime

            //if (x && yPrime) || (y && xPrime)
            /*
             * (x && y)                             | checks if any corners overlap
             * ((x && yPrime) || (y && xPrime))     | checks for intersection
             * ((xPrime || yPrime) && (x && y))     | checks if existing rectangle is inside checking rectangle
             * ((x || y) && (xPrime && yPrime))     | checks if checking rectangle is inside existing rectangle
             */

            /*
            * (x && y)                             | checks if any of the checking rectangles angles are within the existing rectangle
            * ((x && yPrime) || (y && xPrime))     | checks if any of the checking rectangles edges bisect the existing rectangle
            * ((xPrime || yPrime) && (x && y))     | checks if any of the checking rectangles edges are within the existing rectangle and if
            * ((x || y) && (xPrime && yPrime))     | checks if checking rectangle is inside existing rectangle
            */

            bool result = true;
            //check for intersection
            if (!((x[0] && yPrime[0]) || (y[0] && xPrime[0])))
            {
                //if no intersection check for corner overlap
                if (!(x[0] && y[0]))
                {
                    //if no corner overlap check if either rectangle is surrounded
                    if (!((xPrime[1] && yPrime[1]) || (x[1] && y[1])))
                    {
                        result = false;
                    }
                    else
                    {
                        Debug.Log("One surrounds the other");
                    }
                }
                else
                {
                    Debug.Log("Corner overlap found");
                }
            }
            else
            {
                Debug.Log("Intersection found");
            }
            Debug.Log("Intersection result: " + result);

            return result;
        }

        public static List<bool> orResults(float n1, float n2, float toCheck1, float toCheck2)
        {
            List<bool> result = new List<bool>();
            bool eval1 = floatInRangeInclusive(n1, n2, toCheck1);
            bool eval2 = floatInRangeInclusive(n1, n2, toCheck2);
            result.Add(eval1 || eval2);
            result.Add(eval1 && eval2);
            return result;
        }

        public static bool floatInRangeInclusive(float n1, float n2, float toCheck)
        {
            bool result = false;
            if (n1 == n2)
            {
                result = (toCheck == n1);
            }
            else
            {
                float top, bottom;
                if (n1 > n2)
                {
                    top = n1;
                    bottom = n2;
                }
                else
                {
                    top = n2;
                    bottom = n1;
                }
                if (toCheck >= bottom && toCheck <= top)
                {
                    result = true;
                }
            }

            return result;
        }

    }

    [SerializeField]
    Vector2Int size;
    [SerializeField]
    int roomCount;
    [SerializeField]
    Vector2Int roomMaxSize;
    [SerializeField]
    GameObject cubePrefab;
    [SerializeField]
    Material redMaterial;
    [SerializeField]
    Material blueMaterial;
    [SerializeField]
    Material greenMaterial;
    [SerializeField]
    Material purpleMaterial;
    [SerializeField]
    Material grayMaterial;
    [SerializeField]
    GameObject[] roomList;
    [SerializeField]
    GameObject roomParentPrefab;

    System.Random rand1;
    System.Random rand2;
    Grid2D<CellType> grid;
    List<Room> rooms;
    protected KdTree<RandomWalk> kdAnchors = new KdTree<RandomWalk>();
    protected KdTree<RandomWalk> kdRooms = new KdTree<RandomWalk>();
    Delaunay2D delaunay;
    HashSet<Prim.Edge> selectedEdges;
    List<Material> roomColorList;
    private HashSet<Transform> availableAnchors = new HashSet<Transform>();
    private HashSet<Vector3> availableAnchorPos = new HashSet<Vector3>();
    private HashSet<Transform> inUseAnchors = new HashSet<Transform>();

    private List<Vector2Int> roomOptions = new List<Vector2Int>();

    private Dictionary<int, List<int>> roomCollectionDb;
    private int placedRooms;

        //Check if a cube was previously created. If so, check if it proced. If no, assign to new room collection. Deactivate previous cube collider. Create cube. assign cube a number. Place cube.
        //If cube procs, get collision number and join it to the room collection.
        //Need a last check for unassigned cubes.
        //Dictionary<roomNumber, cubeNumber[]> roomCollectionDb
        //Dictionary<cubeNumber, roomNumber> roomDb

    void Start() {
        initVar();           
    }

    private void Update()
    {
        
    }

    public void StartGen()
    {
        Generate();
    }

    private void OnTriggerStay(Collider other)
    {
        Debug.Log("Stay");
    }

    void initVar()
    {
        roomOptions.Add(new Vector2Int(2, 3));
        roomOptions.Add(new Vector2Int(4, 4));
        roomOptions.Add(new Vector2Int(2, 2));
        roomCollectionDb = new Dictionary<int, List<int>>();
        placedRooms = 0;
        roomColorList = new List<Material>();
        roomColorList.Add(redMaterial);
        roomColorList.Add(greenMaterial);
        roomColorList.Add(purpleMaterial);
        roomColorList.Add(grayMaterial);
    }

    void Generate() {
        rand1 = new System.Random();
        StartCoroutine(Wait());
        StopCoroutine(Wait());
        rand2 = new System.Random();
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();

        PlaceRooms();
        Triangulate();
        CreateHallways();
        PathfindHallways();
        PrintRooms();
    }

    public void StartPlaceRooms()
    {
        PlaceRooms();
    }

    public void StartTriangulate()
    {
        Triangulate();
    }

    public void StartCreateHallways()
    {
        CreateHallways();
    }

    public void StartPathfindHallways()
    {
        PathfindHallways();
        PrintRooms();
    }

    private void PrintRooms()
    {
        Debug.Log("Room Collections: " + roomCollectionDb.Count);
    }

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(5);
    }

    public void Setup()
    {
        rand1 = new System.Random();
        StartCoroutine(Wait());
        StopCoroutine(Wait());
        rand2 = new System.Random();
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();
    }

    public void FindClosest()
    {
        KdTree<RandomWalk> temp = new KdTree<RandomWalk>();
        
        foreach (var anchor in kdAnchors)
        {
            temp = kdAnchors;
            temp.RemoveAll(x => x.transform.parent == anchor.transform.parent);
            RandomWalk nearestObj = temp.FindClosest(anchor.transform.position);
            Debug.DrawLine(anchor.transform.position, nearestObj.transform.position, Color.red);
        }

    }

    public void PlaceOneRoom()
    {
        if (availableAnchors.Count != 0)
        {
            int randVal = rand1.Next(-5, 4);
            if (randVal > 0)
            {
                PlaceOneUnattachedRoom();
            }
            else
            {
                PlaceOneAttachedRoom();
            }
        } else
        {
            PlaceOneUnattachedRoom();
        }

    }

    Transform GetRandomAnchor(GameObject target)
    {
        List<string> names = new List<string>()
        {
            "North",
            "South",
            "East",
            "West"
        };
        List<Transform> newAnchors = target.GetComponentsInChildren<Transform>().Where(x => x.transform.childCount == 1).ToList<Transform>();
        List<Transform> modifiedAnchors = new List<Transform>();
        foreach (Transform transform in newAnchors)
        {
            //Don't keep the positions of each box, just their anchors
            if (!(transform.position == target.transform.position || names.Contains(transform.name)))
            {
                modifiedAnchors.Add(transform);
            }
        }
        return modifiedAnchors[UnityEngine.Random.Range(0, modifiedAnchors.Count)];
    }

    public void AttachRoom(Transform sourceAnchor, Transform targetAnchor, Transform room)
    {
        string sourceCardinal = sourceAnchor.parent.name;
        string targetCardinal = targetAnchor.parent.name;

        if (sourceCardinal == "North")
        {
            room.forward = targetAnchor.forward;
        }
        if (sourceCardinal == "South")
        {
            room.forward = -targetAnchor.forward;
        }
        if (sourceCardinal == "East")
        {
            room.right = targetAnchor.forward;
        }
        if (sourceCardinal == "West")
        {
            room.right = -targetAnchor.forward;

        }

        Debug.Log("Moving Room");
        Vector3 offset = sourceAnchor.position - targetAnchor.position;
        room.position -= offset;
    }

    public void PlaceOneAttachedRoom()
    {

        //Get a random anchor from the list of available anchors
        Transform targetAnchor = GetRandomAvailablePosition(availableAnchors);
        if (targetAnchor != null)
        {
            Vector2Int location = new Vector2Int(
            (int)targetAnchor.position.x,
            (int)targetAnchor.position.z
            );

            int rand = rand1.Next(0, roomOptions.Count);
            Vector2Int roomSize = roomOptions[rand];
            string targetAnchorOrientation = targetAnchor.parent.name;
            Transform targetRoom = targetAnchor.parent.parent;
            Transform targetParentRoom = targetAnchor.root;

            bool intersect = false;
            bool outOfBounds = false;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location, roomSize);

            buffer.roomId = placedRooms;
            buffer.roomPrefab = PlaceAttachedRoomFromList(buffer.bounds.position, roomList[rand], roomColorList[rand], targetParentRoom.gameObject);
            Vector3 originalForward = buffer.roomPrefab.transform.forward;
            Transform sourceAnchor = GetRandomAnchor(buffer.roomPrefab);
            string sourceAnchorOrientation = targetAnchor.parent.name;
            AttachRoom(sourceAnchor, targetAnchor, buffer.roomPrefab.transform);
            Vector2Int newLocation = new Vector2Int((int)buffer.roomPrefab.transform.position.x, (int)buffer.roomPrefab.transform.position.z);
            if (buffer.roomPrefab.transform.forward != originalForward)
            {
                roomSize = new Vector2Int(roomSize.y, roomSize.y);
            } //add reverse forward == -forward
            buffer.Reconstruct(newLocation, roomSize);

            if (buffer.bounds.xMin < 0 || buffer.bounds.xMax >= size.x
                || buffer.bounds.yMin < 0 || buffer.bounds.yMax >= size.y)
            {
                outOfBounds = true;
            }
            else
            {
                foreach (var room in rooms)
                {
                    if (Room.Intersect(room, buffer))
                    {
                        intersect = true;
                        Debug.Log("Room #" + newRoom.roomId + " denied");
                        break;
                    }
                    else
                    {
                        Debug.Log("Room #" + newRoom.roomId + " accepted");
                    }
                }
            }


            if (intersect || outOfBounds)
            {
                Destroy(buffer.roomPrefab);
            }
            else
            {
                Debug.Log("(attached) Adding room #" + newRoom.roomId);
                newRoom = buffer;
                UpdateAnchors(newRoom.roomPrefab.transform, targetRoom);
                CubeProperties cubeProperties = newRoom.roomPrefab.GetComponent<CubeProperties>();
                Renderer anchorRender = sourceAnchor.GetComponentInChildren<Renderer>();
                anchorRender.material.color = Color.black;
                RandomWalk prop = sourceAnchor.GetComponent<RandomWalk>();
                CubeProperties targetRoomProperties = targetRoom.gameObject.GetComponent<CubeProperties>();

                anchorRender = targetAnchor.GetComponentInChildren<Renderer>();
                prop = targetAnchor.GetComponent<RandomWalk>();
                prop.associatedRoomID = newRoom.roomId;
                prop.associatedRoomID = targetRoomProperties.roomId;
                anchorRender.material.color = Color.magenta;

                cubeProperties.max = newRoom.bounds.max;
                cubeProperties.min = newRoom.bounds.min;
                cubeProperties.position = newRoom._location;
                placedRooms++;
                cubeProperties.roomId = newRoom.roomId;
                cubeProperties.roomCollectionId = newRoom.roomCollectionId;
                rooms.Add(newRoom);


                foreach (var pos in newRoom.bounds.allPositionsWithin)
                {
                    grid[pos] = CellType.Room;
                }
            }
        }
        else
        {
            PlaceOneUnattachedRoom();
        }
    }
    public void PlaceOneUnattachedRoom()
    {
        Vector2Int location = new Vector2Int(
                rand1.Next(0, size.x),
                rand2.Next(0, size.y)
            );

        int rand = rand1.Next(0, roomOptions.Count);
        Vector2Int roomSize = roomOptions[rand];

        bool add = true;
        Room newRoom = new Room(location, roomSize);
        Room buffer = new Room(location, roomSize);


        if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x
            || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y)
        {
            add = false;
        }
        else
        {
            foreach (var room in rooms)
            {
                if (Room.Intersect(room, buffer))
                {
                    add = false;
                    break;
                }
            }
        }

        if (add)
        {
            Debug.Log("(unattached) Adding room #" + placedRooms);
            newRoom.roomId = placedRooms;
            newRoom.roomPrefab = PlaceRoomFromList(newRoom.bounds.position, roomList[rand], roomColorList[rand]);
            StoreAnchors(newRoom);
            buffer.roomPrefab = PlaceHitbox(new Vector3(0, -1, 0), newRoom.hitboxLocalScale, roomColorList[3]);
            buffer.roomPrefab.transform.SetParent(newRoom.roomPrefab.transform);
            buffer.roomPrefab.transform.localPosition = new Vector3(0, -1, 0);
            CubeProperties cubeProperties = newRoom.roomPrefab.GetComponent<CubeProperties>();
            cubeProperties.max = newRoom.bounds.max;
            cubeProperties.min = newRoom.bounds.min;
            cubeProperties.position = newRoom._location;
            cubeProperties.type = "attached";
            placedRooms++;

            cubeProperties.roomId = newRoom.roomId;
            cubeProperties.roomCollectionId = newRoom.roomCollectionId;
            rooms.Add(newRoom);

            foreach (var pos in newRoom.bounds.allPositionsWithin)
            {
                grid[pos] = CellType.Room;
            }
        }
    }



    void PlaceRooms() {
        for (int i = 0; i < roomCount; i++) {
            PlaceOneRoom();
        }
    }

    void StoreAnchors(Room newRoom)
    {
        List<Transform> newAnchors = newRoom.roomPrefab.GetComponentsInChildren<Transform>().ToList<Transform>();
        foreach (Transform transform in newAnchors)
        {
            //Don't keep the positions of each box, just their anchors
            if (!availableAnchors.Contains(transform) && (transform.position != newRoom.roomPrefab.transform.position) && CheckAnchorInBounds(transform))
            {
                availableAnchors.Add(transform);
            }                
        }
    }

    void Triangulate() {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in rooms) {
            vertices.Add(new Vertex<Room>((Vector2)room.bounds.position + ((Vector2)room.bounds.size) / 2, room));
        }

        delaunay = Delaunay2D.Triangulate(vertices);
    }

    void CreateHallways() {

        //TODO: Stop hallways from forming at the edges
        
        List<Prim.Edge> edges = new List<Prim.Edge>();

        foreach (var edge in delaunay.Edges) {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U);

        selectedEdges = new HashSet<Prim.Edge>(mst);
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (var edge in remainingEdges) {
            if (rand2.NextDouble() < 0.125) {
                selectedEdges.Add(edge);
            }
        }
    }

    void PathfindHallways() {
        //TODO: Evaluate connected rooms as a single room then create hallways
        DungeonPathfinder2D aStar = new DungeonPathfinder2D(size);

        foreach (var edge in selectedEdges) {
            var startRoom = (edge.U as Vertex<Room>).Item;
            var endRoom = (edge.V as Vertex<Room>).Item;

            var startPosf = startRoom.bounds.center;
            var endPosf = endRoom.bounds.center;
            var startPos = new Vector2Int((int)startPosf.x, (int)startPosf.y);
            var endPos = new Vector2Int((int)endPosf.x, (int)endPosf.y);

            var path = aStar.FindPath(startPos, endPos, (DungeonPathfinder2D.Node a, DungeonPathfinder2D.Node b) => {
                var pathCost = new DungeonPathfinder2D.PathCost();
                
                pathCost.cost = Vector2Int.Distance(b.Position, endPos);    //heuristic

                if (grid[b.Position] == CellType.Room) {
                    pathCost.cost += 10;
                } else if (grid[b.Position] == CellType.None) {
                    pathCost.cost += 5;
                } else if (grid[b.Position] == CellType.Hallway) {
                    pathCost.cost += 1;
                }

                pathCost.traversable = true;

                return pathCost;
            });

            if (path != null) {
                for (int i = 0; i < path.Count; i++) {
                    var current = path[i];

                    if (grid[current] == CellType.None) {
                        grid[current] = CellType.Hallway;
                    }

                    if (i > 0) {
                        var prev = path[i - 1];

                        var delta = current - prev;
                    }
                }

                foreach (var pos in path) {
                    if (grid[pos] == CellType.Hallway) {
                        PlaceHallway(pos);
                    }
                }
            }
        }
    }

    GameObject PlaceHitbox(Vector3 location, Vector3 size, Material material)
    {


        GameObject go = Instantiate(cubePrefab, location, Quaternion.identity);
        //go.transform.position = go.GetComponent<Renderer>().bounds.center;


        //Update properties with cube number

        go.GetComponent<Transform>().localScale = size;

        go.GetComponent<MeshRenderer>().material = material;

        return go;
    }

    GameObject PlaceCube(Vector2Int location, Vector2Int size, Material material) {

        
        GameObject go = Instantiate(cubePrefab, new Vector3(location.x, 0, location.y), Quaternion.identity);
        go.transform.position = go.GetComponent<Renderer>().bounds.center;

        //Update properties with cube number

        go.GetComponent<Transform>().localScale = new Vector3(size.x, 1, size.y);
        
        go.GetComponent<MeshRenderer>().material = material;

        return go;
    }

    void UpdateAnchors(Transform sourceRoom, Transform targetRoom)
    {
        //Remove unavailable anchors and add new anchors
        List<string> names = new List<string>()
        {
            "North",
            "South",
            "East",
            "West"
        };
        List<Transform> sourceAnchors = sourceRoom.GetComponentsInChildren<Transform>().Where(x => x.childCount == 1).ToList<Transform>();
        List<Transform> targetAnchors = targetRoom.GetComponentsInChildren<Transform>().Where(x => x.childCount == 1).ToList<Transform>();
        List<Transform> freeAnchors = availableAnchors.ToList<Transform>();
        List<Transform> anchorBuffer = new List<Transform>();

        foreach (Transform anchor in sourceAnchors)
        {
            if (!(anchor.position == sourceRoom.position || names.Contains(anchor.name)))
            {
                anchorBuffer.Add(anchor);
            }
        }
        foreach (Room room in rooms)
        {
            foreach (Transform sourceAnchor in anchorBuffer)
            {
                if (CheckAnchorInBounds(sourceAnchor))
                {
                    if (!freeAnchors.Contains(sourceAnchor))
                    {
                        Room source = new Room(new Vector2Int((int)sourceAnchor.position.x, (int)sourceAnchor.position.z), new Vector2Int(1, 1));
                        if (!Room.Intersect(room, source))
                        {
                            freeAnchors.Add(sourceAnchor);
                        }
                    }
                }
            }
            foreach (Transform targetAnchor in targetAnchors)
            {
                Room target = new Room(new Vector2Int((int)targetAnchor.position.x, (int)targetAnchor.position.z), new Vector2Int(1, 1));
                if (Room.Intersect(room, target))
                {
                    if (freeAnchors.Contains(targetAnchor))
                    {
                        freeAnchors.Remove(targetAnchor);
                    }
                }
            }
        }

        //foreach (Transform transform in freeAnchors)
        //{
        //    //Add new anchors
        //    if (!availableAnchors.Contains(transform) && !availableAnchorPos.Contains(transform.position) && CheckAnchorInBounds(transform))
        //    {
        //        availableAnchors.Add(transform);
        //        availableAnchorPos.Add(transform.position);
        //    }
        //}
    }


    bool CheckAnchorInBounds(Transform anchor)
    {
        Room anchorRoom = new Room(new Vector2Int((int)anchor.position.x, (int)anchor.position.z), new Vector2Int(1, 1));
        bool inBounds = true;
        if (
            anchorRoom.bounds.xMin < 0 || anchorRoom.bounds.xMax >= size.x
            || anchorRoom.bounds.yMin < 0 || anchorRoom.bounds.yMax >= size.y)
        {
            inBounds = false;
        }
        return inBounds;
    }

    private Transform GetRandomAvailablePosition(HashSet<Transform> availableAnchors)
    {
        Transform result;
        // Get a random position from the occupied positions set

        HashSet<Transform> temp = availableAnchors;
        temp.ExceptWith(inUseAnchors);
        List<Transform> availableAnchorsList = new List<Transform>(temp);
        Transform potentialAnchor = availableAnchorsList[UnityEngine.Random.Range(0, availableAnchorsList.Count)];

        if (inUseAnchors.Contains(potentialAnchor))
        {
            temp.Remove(potentialAnchor);
            if (temp.Count > 0)
            {
                result = GetRandomAvailablePosition(temp);
            }
            else
            {
                result = null;
            }
        }
        else
        {
            result = potentialAnchor;
        }
        return result;
    }

    GameObject PlaceSpecificRoom(Vector2Int location, GameObject room, Material material, GameObject parent)
    {

        Vector3 v3Loc = new Vector3(location.x, 0, location.y);
        GameObject go = Instantiate(room, v3Loc, Quaternion.identity);
        go.transform.SetParent(parent.transform);
        //go.GetComponent<Renderer>().bounds.center = parent.transform.position;


        //Update properties with cube number

        //go.GetComponent<Transform>().localScale = new Vector3(size.x, 1, size.y);

        go.GetComponent<MeshRenderer>().material = material;

        return go;
    }

    GameObject PlaceRoomFromList(Vector2Int location, GameObject room, Material material)
    {
        GameObject parent = PlaceRoomParent(location);
        GameObject go = PlaceSpecificRoom(location, room, material, parent);
        return go;
    }

    GameObject PlaceAttachedRoomFromList(Vector2Int location, GameObject room, Material material, GameObject parent)
    {
        GameObject go = PlaceSpecificRoom(location, room, material, parent);
        return go;
    }

    GameObject PlaceRoomParent(Vector2Int location)
    {
        Vector3 v3Loc = new Vector3(location.x, 0, location.y);
        GameObject go = Instantiate(roomParentPrefab, v3Loc, Quaternion.identity);
        return go;
    }


    void PlaceHallway(Vector2Int location) {
        GameObject go = PlaceCube(location, new Vector2Int(1, 1), blueMaterial);
        go.GetComponent<CubeProperties>().isHallway = true;
    }
}
