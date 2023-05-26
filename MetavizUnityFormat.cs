using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

/**
 * MetavizTransform
 */

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

/**
 * MetavizNode
 */

public class MetavizNode
{

    public string id;
    public string type;
    public MetavizTransform transform;
    public Dictionary<string, object> data;
    public List<MetavizLink> links;

    public MetavizNode(string in_id, string in_type, int in_x, int in_y, int in_w, int in_h, Dictionary<string, object> in_data)
    {
        links = new List<MetavizLink>();
        id = in_id;
        type = in_type;
        transform = new MetavizTransform(in_x, in_y, in_w, in_h);
        data = in_data;
    }

    public MetavizNode[] GetChildren()
    {
        List<MetavizNode> children = new List<MetavizNode>();
        foreach (MetavizLink link in links)
        {
            children.Add(link.end);
        }
        return children.ToArray();
    }

    public void TraverseTree(Action<MetavizNode> callback, int max = 1000, int level = 1)
    {
        foreach (MetavizNode node in GetChildren())
        {
            callback(node);
            if (level < max) node.TraverseTree(callback, level + 1);
        }
    }

#if UNITY_EDITOR
    public string DebugDump()
    {
        string buffer = "Node:\n";
        buffer += "  id: " + id + "\n";
        buffer += "  type: " + type + "\n";
        buffer += "  transform: (x = " + transform.x + ", y = " + transform.y + ", w = " + transform.w + ", h = " + transform.h + ")\n";
        buffer += "  data (" + data.Count + ")\n";
        foreach (KeyValuePair<string, object> entry in data)
        {
            buffer += "    " + entry.Key + " = " + (string)entry.Value + "\n";
        }
        buffer += "  links (" + links.Count + ")\n";
        foreach (MetavizLink link in links)
        {
            buffer += "    -> " + link.end.id + "\n";
        }
        return buffer;
    }
#endif

}

/**
 * MetavizNodes
 */

public class MetavizNodes
{

    List<MetavizNode> list;

    public MetavizNodes()
    {
        list = new List<MetavizNode>();
    }

    public MetavizNode Add(string id, string type, int x, int y, int w, int h, Dictionary<string, object> data)
    {
        MetavizNode node = new MetavizNode(id, type, x, y, w, h, data);
        list.Add(node);
        return node;
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

/**
 * MetavizLink
 */

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

/**
 * MetavizLinks
 */

public class MetavizLinks
{

    List<MetavizLink> list;

    public MetavizLinks()
    {
        list = new List<MetavizLink>();
    }

    public MetavizLink Add(string id, string type, MetavizNode start, MetavizNode end)
    {
        MetavizLink link = new MetavizLink(id, type, start, end);
        list.Add(link);
        return link;
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

/**
 * MetavizRender
 */

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

/**
 * MetavizUnityFormat
 */

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
                        foreach (KeyValuePair<string, object> entry in DataCollect(packet.Attributes))
                        {
                            node.data[entry.Key] = entry.Value;
                        }
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
                            MetavizLink link = render.links.Add(
                                packet.GetAttribute("link"),
                                packet.GetAttribute("type"),
                                start,
                                end
                            );
                            start.links.Add(link);
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
                data[attribute.Name.Substring(5)] = System.Net.WebUtility.HtmlDecode(attribute.Value);
            }
        }
        return data;
    }

}
