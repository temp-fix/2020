using SharpNeat.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;

/// <summary>
/// Static class for reading and writing SensorConfig(s) to and from XML.
/// </summary>
using UnityEngine;


public static class SensorConfigXmlIO
{
	#region Constants [XML Strings]
	
	const string __ElemRoot = "root";
	const string __ElemPopulation = "pop";
	const string __ElemIndividual = "indiv";
	const string __ElemSensor = "sensor";
	
	const string __AttrId = "id";
	const string __AttrFitness = "fit";
	const string __AttrVertical = "vert";
	const string __AttrHorizontal = "hor";
    const string __AttrSensorVertical = "sensor_vert";
    const string __AttrSensorHorizontal = "sensor_hor";
    const string __AttrSensorType = "sensor_type";
    const string __AttrSensorRange = "range";
    const string __AttrSensorFOV = "fov";

    #endregion


    #region Public Static Methods [Write to XML]

    public static void WriteComplete(XmlWriter xw, GameObject[] population)
	{
		if(population.Length == 0)
		{   // Nothing to do.
			return;
		}
		
		// <root>
		xw.WriteStartElement(__ElemRoot);

		// <pop>
		xw.WriteStartElement(__ElemPopulation);
		
		// Write genomes.
		foreach(GameObject individual in population) {
			Write(xw, individual.GetComponent<SensorConfigIndividual>());
		}
		
		// </pop>
		xw.WriteEndElement();
		
		// </root>
		xw.WriteEndElement();
	}

	public static void WriteComplete(XmlWriter xw, SensorConfigIndividual genome)
	{
		// <root>
		xw.WriteStartElement(__ElemRoot);
		
		// <pop>
		xw.WriteStartElement(__ElemPopulation);
		
		// Write single genome.
		Write(xw, genome);
		
		// </pop>
		xw.WriteEndElement();
		
		// </root>
		xw.WriteEndElement();
	}

	public static void Write(XmlWriter xw, SensorConfigIndividual genome)
	{
		// <indiv>
		xw.WriteStartElement(__ElemIndividual);
//		xw.WriteAttributeString(__AttrId, genome.Id.ToString(NumberFormatInfo.InvariantInfo));
		xw.WriteAttributeString(__AttrFitness, genome.Fitness + "");
		
		// Emit nodes.
		StringBuilder sb = new StringBuilder();
		foreach(SensorConfigProperties Sensors in genome.Genes.Genome)
		{
			//<sensor />
			xw.WriteStartElement(__ElemSensor);
            xw.WriteAttributeString(__AttrSensorType, Sensors.sensorType.ToString());
			xw.WriteAttributeString(__AttrVertical, Sensors.angles.x + "");
			xw.WriteAttributeString(__AttrHorizontal, Sensors.angles.y + "");
            xw.WriteAttributeString(__AttrSensorVertical, Sensors.direction.x + "");
            xw.WriteAttributeString(__AttrSensorHorizontal, Sensors.direction.y + "");
            xw.WriteAttributeString(__AttrSensorRange, Sensors.Range + "");
            xw.WriteAttributeString(__AttrSensorFOV, Sensors.FOV + "");
            xw.WriteEndElement();
		}

		// </indiv>
		xw.WriteEndElement();
	}

    #endregion

    #region Public Static Methods [Read from XML]
    public static SensorConfigGenome ReadGenome(XmlReader xr)
    {
        // Find <indiv>.
        XmlIoUtils.MoveToElement(xr, true, __ElemIndividual);

        // Create a reader over the <indiv> sub-tree.
        List<SensorConfigProperties> sensors = new List<SensorConfigProperties>();

        using (XmlReader xrSubtree = xr.ReadSubtree())
        {
            // Re-scan for the root <indiv> element.
            XmlIoUtils.MoveToElement(xrSubtree, false);

            // Move to first node elem.
            XmlIoUtils.MoveToElement(xrSubtree, true, __ElemSensor);

            // Read node elements.
            do
            {
                float vertical = XmlIoUtils.ReadAttributeAsFloat(xrSubtree, __AttrVertical);
                float horizontal = XmlIoUtils.ReadAttributeAsFloat(xrSubtree, __AttrHorizontal);
                float sensorVertical = XmlIoUtils.ReadAttributeAsFloat(xrSubtree, __AttrSensorVertical);
                float sensorHorizontal = XmlIoUtils.ReadAttributeAsFloat(xrSubtree, __AttrSensorHorizontal);
                float range = XmlIoUtils.ReadAttributeAsFloat(xrSubtree, __AttrSensorRange);
                float fov = XmlIoUtils.ReadAttributeAsFloat(xrSubtree, __AttrSensorFOV);
                string sensorType = xrSubtree.GetAttribute(__AttrSensorType);
                sensors.Add(new SensorConfigProperties(new Vector2(vertical, horizontal), new Vector3(sensorVertical, sensorHorizontal), range, fov, (SensorType) Enum.Parse(typeof(SensorType), sensorType)));
            }
            while (xrSubtree.ReadToNextSibling(__ElemSensor));
        }

        return new SensorConfigGenome(sensors.ToArray());
    }

    #endregion
}
