using UnityEngine;
using UnityEngine.UI;

public class StartGen : MonoBehaviour
{

    [SerializeField] GameObject generator;
    [SerializeField] private Button myButton;
    private Generator2D generator2D;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        myButton.onClick.AddListener(() =>
        {
            if (myButton.name == "PlaceRoomsBtn")
            {
                generator.GetComponent<BasicGenerator2D>().StartPlaceRooms();
            }
            else if (myButton.name == "FindClosestBtn")
            {
                generator.GetComponent<Generator2D>().FindClosest();
            }
            else if (myButton.name == "SetupBtn")
            {
                generator.GetComponent<BasicGenerator2D>().Setup();
            }
            else if (myButton.name == "PlaceOneRoomBtn")
            {
                generator.GetComponent<BasicGenerator2D>().PlaceOneRoom();
            }
            else if (myButton.name == "TriangulateBtn")
            {
                generator.GetComponent<Generator2D>().StartTriangulate();
            }
            else if (myButton.name == "CreateHallwaysBtn")
            {
                generator.GetComponent<Generator2D>().StartCreateHallways();
            }
            else if (myButton.name == "PathfindHallwaysBtn")
            {
                generator.GetComponent<Generator2D>().StartPathfindHallways();
            }
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
