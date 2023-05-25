using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class MetavizTransform
{
    public int x;
    public int y;
    public int w;
    public int h;

    public MetavizTransform(int in_x, int in_y, int in_w, int in_h)
    {
        x = in_x;
        y = in_y;
        w = in_w;
        h = in_h;
    }

}

public class MetavizNode
{

    public string id;
    public string type;
    public MetavizTransform transform;
    public Dictionary<string, object> data;

    public MetavizNode(string in_id, string in_type, int in_x, int in_y, int in_w, int in_h, Dictionary<string, object> in_data)
    {
        id = in_id;
        type = in_type;
        transform = new MetavizTransform(in_x, in_y, in_w, in_h);
        data = in_data;
    }

    public MetavizNode[] GetChildren()
    {
        return null;
    }

}

public class MetavizNodes
{

    List<MetavizNode> list;

    public MetavizNodes()
    {
        list = new List<MetavizNode>();
    }

    public void Add(string id, string type, int x, int y, int w, int h, Dictionary<string, object> data)
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

    public MetavizNode Get(string type, string key, string val)
    {
        foreach (MetavizNode node in list)
        {
            if (node.type == type)
            {
                foreach (KeyValuePair<string, object> pair in node.data)
                {
                    if (pair.Key.Equals(key) && pair.Value.Equals(val)) return node;
                }
            }
        }
        return null;
    }

    public MetavizNode[] GetAll(string ids)
    {
        List<MetavizNode> nodes = new List<MetavizNode>();
        foreach (string id in ids.Split(','))
        {
            MetavizNode node = Get(id);
            if (node != null) nodes.Add(node);
        }
        return nodes.ToArray();
    }

    public bool Del(string id)
    {
        MetavizNode node = Get(id);
        if (node != null)
        {
            list.Remove(node);
            return true;
        }
        return false;
    }

}

public class MetavizLink
{

    public string id;
    public string type;
    public MetavizNode start;
    public MetavizNode end;

    public MetavizLink(string in_id, string in_type, MetavizNode in_start, MetavizNode in_end)
    {
        id = in_id;
        type = in_type;
        start = in_start;
        end = in_end;
    }

}

public class MetavizLinks
{

    List<MetavizLink> list;

    public MetavizLinks()
    {
        list = new List<MetavizLink>();
    }

    public void Add(string id, string type, MetavizNode start, MetavizNode end)
    {
        list.Add(new MetavizLink(id, type, start, end));
    }

    public MetavizLink Get(string id)
    {
        foreach (MetavizLink link in list)
        {
            if (link.id == id) return link;
        }
        return null;
    }

    public MetavizLink[] GetAll(string ids)
    {
        List<MetavizLink> links = new List<MetavizLink>();
        foreach (string id in ids.Split(','))
        {
            MetavizLink link = Get(id);
            if (link != null) links.Add(link);
        }
        return links.ToArray();
    }

    public bool Del(string id)
    {
        MetavizLink link = Get(id);
        if (link != null)
        {
            list.Remove(link);
            return true;
        }
        return false;
    }

}

public class MetavizRender
{

    public MetavizNodes nodes;
    public MetavizLinks links;

    public MetavizRender()
    {
        nodes = new MetavizNodes();
        links = new MetavizLinks();
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
        if (format != "MetavizStack" || version != 4)
        {
            Debug.LogError("Unsupported or unknown Metaviz format version!");
            return;
        }

        // All packets
        List<XmlElement> packets = new List<XmlElement>();

        // Flatten sessions to single packets list
        foreach (XmlElement session in xmlDoc.SelectNodes("mv/history/session"))
        {
            foreach (XmlElement packet in session.ChildNodes) packets.Add(packet);
        }

        // Sort packets by timestamp
        packets.Sort((a, b) => a.GetAttribute("timestamp").CompareTo(b.GetAttribute("timestamp")));

        // Process packets for nodes
        foreach (XmlElement packet in packets)
        {

            MetavizNode node = packet.HasAttribute("node") ? render.nodes.Get(packet.GetAttribute("node")) : null;
            MetavizNode[] nodes = packet.HasAttribute("nodes") ? render.nodes.GetAll(packet.GetAttribute("nodes")) : null;

            switch (packet.Name)
            {

                case "add":
                    // Node
                    if (packet.HasAttribute("node"))
                        render.nodes.Add(
                            packet.GetAttribute("node"),
                            packet.GetAttribute("type"),
                            int.Parse(packet.GetAttribute("x")),
                            int.Parse(packet.GetAttribute("y")),
                            int.Parse(packet.GetAttribute("w")),
                            int.Parse(packet.GetAttribute("h")),
                            DataCollect(packet.Attributes)
                        );
                    break;

                case "del":
                    // Node
                    if (packet.HasAttribute("node"))
                        render.nodes.Del(packet.GetAttribute("node"));
                    break;

                case "move":
                    foreach (MetavizNode n in nodes)
                    {
                        if (packet.HasAttribute("position-x"))
                        {
                            n.transform.x = int.Parse(packet.GetAttribute("position-x"));
                            n.transform.y = int.Parse(packet.GetAttribute("position-y"));
                        }
                        else if (packet.HasAttribute("offset"))
                        {
                            n.transform.x += int.Parse(packet.GetAttribute("offset-x"));
                            n.transform.y += int.Parse(packet.GetAttribute("offset-y"));
                        }
                    }
                    break;

                case "resize":
                    foreach (MetavizNode n in nodes)
                    {
                        n.transform.w = int.Parse(packet.GetAttribute("w"));
                        n.transform.h = int.Parse(packet.GetAttribute("h"));
                    }
                    break;

                case "param":
                    if (node != null)
                    {
                        node.data = DataCollect(packet.Attributes);
                    }
                    break;

            }
        } // foreach

        // Process packets for links (when all nodes already exists)
        foreach (XmlElement packet in packets)
        {

            switch (packet.Name)
            {

                case "add":
                    // Link
                    if (packet.HasAttribute("link"))
                    {
                        MetavizNode start = render.nodes.Get(packet.GetAttribute("start"));
                        MetavizNode end = render.nodes.Get(packet.GetAttribute("end"));
                        if (start != null && end != null)
                        {
                            render.links.Add(
                                packet.GetAttribute("link"),
                                packet.GetAttribute("type"),
                                start,
                                end
                            );
                        }
                    }
                    break;

                case "del":
                    // Link
                    if (packet.HasAttribute("link"))
                        render.links.Del(packet.GetAttribute("link"));
                    break;

            }
        } // foreach

    }

    Dictionary<string, object> DataCollect(XmlAttributeCollection attributes)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();
        foreach (XmlAttribute attribute in attributes)
        {
            if (attribute.Name.StartsWith("data-"))
            {
                data[attribute.Name.Substring(5)] = attribute.Value;
            }
        }
        return data;
    }

}
