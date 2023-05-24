using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class MetavizNode
{

    public string id;
    public string type;
    public int x;
    public int y;
    public int w;
    public int h;
    public string data;

    public MetavizNode(string in_id, string in_type, int in_x, int in_y, int in_w, int in_h, string in_data)
    {
        id = in_id;
        type = in_type;
        x = in_x;
        y = in_y;
        w = in_w;
        h = in_h;
        data = in_data;
    }

}

public class MetavizNodes
{

    List<MetavizNode> list;

    public MetavizNodes()
    {
        list = new List<MetavizNode>();
    }

    public void Add(string id, string type, int x, int y, int w, int h, string data)
    {
        list.Add(new MetavizNode(id, type, x, y, w, h, data));
    }

    public MetavizNode Get(string id)
    {
        foreach (MetavizNode node in list)
        {
            if (node.id == id) return node;
        }
        return null;
    }

}

public class MetavizRender
{

    public MetavizNodes nodes;

    public MetavizRender()
    {
        nodes = new MetavizNodes();
    }

}


public class MetavizUnityFormat
{

    public MetavizRender render;

    public MetavizUnityFormat()
    {
        render = new MetavizRender();
    }

    public void parse(string buffer)
    {
        // Parse
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(buffer);

        // Header
        string format = xmlDoc.SelectSingleNode("mv/format").InnerText;
        int version = int.Parse(xmlDoc.SelectSingleNode("mv/version").InnerText);
        if (format != "MetavizStack" || version != 3)
        {
            Debug.LogError("Unsupported or unknown Metaviz format version!");
            return;
        }

        // All packets
        List<XmlElement> packets = new List<XmlElement>();

        // Flatten sessions to single packets list
        foreach (XmlElement session in xmlDoc.SelectNodes("mv/history/session"))
        {
            Debug.Log("session " + session.GetAttribute("id"));
            foreach (XmlElement packet in session.ChildNodes) packets.Add(packet);
        }

        // Sort packets by timestamp
        packets.Sort((a, b) => a.GetAttribute("timestamp").CompareTo(b.GetAttribute("timestamp")));

        // Process packets
        foreach (XmlElement packet in packets)
        {
            switch (packet.Name)
            {

                case "add":
                    Debug.Log("add " + packet.GetAttribute("nodes"));
                    // render.nodes.Add();
                    break;

                case "del":
                    break;

                case "move":
                    break;

                case "resize":
                    break;

                case "param":
                    break;

            }
        }
    }

}
