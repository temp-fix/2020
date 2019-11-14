using System.Xml;
using UnityEngine;

public class SensorConfigLoadingTest : MonoBehaviour {

    public GameObject Car;
    public string XMLPath;

    // Use this for initialization
    void Start () {
        var car_gameObj = Instantiate(Car, transform.position, transform.rotation) as GameObject;

        //Set this transform as the parent obj for the car, makes things neat.
        car_gameObj.transform.SetParent(transform);

        if (!car_gameObj.activeSelf)
            car_gameObj.SetActive(true);

        MakeNewIndividualGameObjFromGenome(car_gameObj, loadGenomeFromFile(XMLPath));
    }

    // Update is called once per frame
    void Update () {
	
	}

    private void MakeNewIndividualGameObjFromGenome(GameObject carObject, SensorConfigGenome genome)
    {
        //Attach the individual script to the sensor conifg game object and set its the phenome using the given genome
        carObject.AddComponent<SensorConfigIndividual>().SetSensorConfigPhenomeUsingGenomeSurface(genome, Car);
    }

    private SensorConfigGenome loadGenomeFromFile(string filepath) {
        using (XmlReader xr = XmlReader.Create(filepath))
        {
            return SensorConfigXmlIO.ReadGenome(xr);
        }
    }

}
