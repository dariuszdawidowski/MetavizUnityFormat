/**
 * Metaviz Stacked XML format plugin for Unity3d
 * v0.5.0
 */

namespace Metaviz
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;
    using UnityEngine;

    /**
     * MetavizTransform
     */

    public class Transform
    {
        public int x;
        public int y;
        public int w;
        public int h;

        public Transform(int in_x, int in_y, int in_w, int in_h)
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

    public class Node
    {

        public string id;
        public string type;
        public Transform transform;
        public Dictionary<string, object> data;
        public List<Link> links;

        public Node(string in_id, string in_type, int in_x, int in_y, int in_w, int in_h, Dictionary<string, object> in_data)
        {
            links = new List<Link>();
            id = in_id;
            type = in_type;
            transform = new Transform(in_x, in_y, in_w, in_h);
            data = in_data;
        }

        public Node[] GetChildren()
        {
            List<Node> children = new List<Node>();
            foreach (Link link in links)
            {
                children.Add(link.end);
            }
            return children.ToArray();
        }

        public void TraverseTree(Action<Node> callback, int max = 1000, int level = 1)
        {
            foreach (Node node in GetChildren())
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
            foreach (Link link in links)
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

    public class Nodes
    {

        List<Node> list;

        public Nodes()
        {
            list = new List<Node>();
        }

        public Node Add(string id, string type, int x, int y, int w, int h, Dictionary<string, object> data)
        {
            Node node = new Node(id, type, x, y, w, h, data);
            list.Add(node);
            return node;
        }

        public Node Get(string id)
        {
            foreach (Node node in list)
            {
                if (node.id == id) return node;
            }
            return null;
        }

        public Node Get(string type, string key, string val)
        {
            foreach (Node node in list)
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

        public Node[] GetAll(string ids)
        {
            List<Node> nodes = new List<Node>();
            foreach (string id in ids.Split(','))
            {
                Node node = Get(id);
                if (node != null) nodes.Add(node);
            }
            return nodes.ToArray();
        }

        public bool Del(string id)
        {
            Node node = Get(id);
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

    public class Link
    {

        public string id;
        public string type;
        public Node start;
        public Node end;

        public Link(string in_id, string in_type, Node in_start, Node in_end)
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

    public class Links
    {

        List<Link> list;

        public Links()
        {
            list = new List<Link>();
        }

        public Link Add(string id, string type, Node start, Node end)
        {
            Link link = new Link(id, type, start, end);
            list.Add(link);
            return link;
        }

        public Link Get(string id)
        {
            foreach (Link link in list)
            {
                if (link.id == id) return link;
            }
            return null;
        }

        public Link[] GetAll(string ids)
        {
            List<Link> links = new List<Link>();
            foreach (string id in ids.Split(','))
            {
                Link link = Get(id);
                if (link != null) links.Add(link);
            }
            return links.ToArray();
        }

        public bool Del(string id)
        {
            Link link = Get(id);
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

    public class Render
    {

        public Nodes nodes;
        public Links links;

        public Render()
        {
            nodes = new Nodes();
            links = new Links();
        }

    }

    /**
     * MetavizUnityFormat
     */

    public class UnityFormat
    {

        public Render render;

        public UnityFormat()
        {
            render = new Render();
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

                Node node = packet.HasAttribute("node") ? render.nodes.Get(packet.GetAttribute("node")) : null;
                Node[] nodes = packet.HasAttribute("nodes") ? render.nodes.GetAll(packet.GetAttribute("nodes")) : null;

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
                        foreach (Node n in nodes)
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
                        foreach (Node n in nodes)
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
                            Node start = render.nodes.Get(packet.GetAttribute("start"));
                            Node end = render.nodes.Get(packet.GetAttribute("end"));
                            if (start != null && end != null)
                            {
                                Link link = render.links.Add(
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

}
