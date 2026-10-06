using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Brotherhood
{
    // The authored trial passages are an optional layer over discovered source
    // cells. This component never reveals cells or changes the player's pins.
    public sealed class BrotherhoodTrialMapOverlay : MonoBehaviour
    {
        const string OverlayName = "TrialMapOverlay";
        const string VisitedFlag = "D17_TRIAL/VISITED";
        static readonly Vector2 MarkerOffset = new Vector2(-11, -11);
        static readonly Color Gold = new Color(1f, .72f, .26f, .95f);
        static readonly Color Cyan = new Color(.25f, .9f, .98f, .95f);
        readonly Dictionary<string, MarkerView> markers = new Dictionary<string, MarkerView>();
        readonly Dictionary<string, LinkView> links = new Dictionary<string, LinkView>();
        RectTransform pathsRoot, markersRoot, captionRoot;
        Text caption;
        Font labelFont;

        sealed class MarkerView
        {
            public RectTransform root;
            public Image[] edges;
        }

        sealed class LinkView
        {
            public RectTransform root;
            public readonly List<Image> dashes = new List<Image>();
            public Vector2 from, to;
            public bool hasGeometry;
        }

        sealed class CellView
        {
            public SourceMap.Cell cell;
            public bool available;
        }

        sealed class Connection
        {
            public SourceMap.Cell from, to;
            public bool available = true;
        }

        public static void Refresh(RectTransform mapContent, BrotherhoodGame game)
        {
            if (mapContent == null) return;
            var child = mapContent.Find(OverlayName);
            var route = game != null ? game.GetComponent<BrotherhoodTrialRoute>() : null;
            if (route == null || game.progress == null || route.Passages == null)
            {
                if (child != null) child.gameObject.SetActive(false);
                return;
            }

            var overlay = child != null ? child.GetComponent<BrotherhoodTrialMapOverlay>() : null;
            if (overlay == null)
            {
                var holder = child != null ? child.gameObject : new GameObject(OverlayName, typeof(RectTransform), typeof(CanvasGroup));
                holder.transform.SetParent(mapContent, false);
                var rect = holder.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                overlay = holder.AddComponent<BrotherhoodTrialMapOverlay>();
            }
            // Missing Unity components can be managed wrappers that compare equal
            // to null. Use Unity's null check rather than the C# ?? operator.
            var group = overlay.GetComponent<CanvasGroup>();
            if (group == null) group = overlay.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            overlay.Render(game, route);
            var player = mapContent.Find("PlayerMarker");
            if (player != null && overlay.transform.GetSiblingIndex() > player.GetSiblingIndex())
                overlay.transform.SetSiblingIndex(player.GetSiblingIndex());
        }

        void Render(BrotherhoodGame game, BrotherhoodTrialRoute route)
        {
            EnsureUI();
            var source = SourceMap.Load();
            var progress = game.progress;
            var cells = new Dictionary<string, CellView>();
            var connections = new Dictionary<string, Connection>();
            foreach (var passage in route.Passages)
            {
                if ((object)passage == null) continue;
                var from = string.IsNullOrEmpty(passage.room) ? null : source.At(passage.room, passage.position + Vector2.up * .5f);
                var to = string.IsNullOrEmpty(passage.target) ? null : source.At(passage.target, passage.arrival + Vector2.up * .5f);
                bool available = string.IsNullOrEmpty(passage.requiredFlag) || progress.HasFlag(passage.requiredFlag);
                AddCell(cells, from, available);
                AddCell(cells, to, available);
                if (from == null || to == null || from.ngPlus || to.ngPlus || from.key == to.key) continue;
                string key = string.CompareOrdinal(from.key, to.key) < 0 ? from.key + "|" + to.key : to.key + "|" + from.key;
                if (!connections.TryGetValue(key, out var connection))
                    connections[key] = connection = new Connection { from = from, to = to };
                // A two-way link stays gold while either authored direction is locked.
                connection.available &= available;
            }

            foreach (var marker in markers.Values) if (marker.root != null) marker.root.gameObject.SetActive(false);
            foreach (var link in links.Values) if (link.root != null) link.root.gameObject.SetActive(false);
            SourceMap.Cell labelCell = null, entranceCell = null;
            int visible = 0;
            foreach (var entry in cells)
            {
                var data = entry.Value;
                if (!source.Discovered(progress, data.cell.key)) continue;
                if (!markers.TryGetValue(entry.Key, out var marker) || !ValidMarker(marker)) markers[entry.Key] = marker = CreateMarker(entry.Key);
                marker.root.anchoredPosition = data.cell.MapPosition + MarkerOffset;
                marker.root.gameObject.SetActive(true);
                bool entrance = data.cell.room == "D17Z01S05";
                foreach (var edge in marker.edges) edge.color = entrance || !data.available ? Gold : Cyan;
                if (entrance) entranceCell = data.cell;
                if (labelCell == null || game.Current != null && data.cell.room == game.Current.id) labelCell = data.cell;
                visible++;
            }
            foreach (var entry in connections)
            {
                var data = entry.Value;
                if (!source.Discovered(progress, data.from.key) || !source.Discovered(progress, data.to.key)) continue;
                if (!links.TryGetValue(entry.Key, out var link) || link.root == null) links[entry.Key] = link = new LinkView { root = MakeRect(pathsRoot, "Passage_" + entry.Key, Vector2.zero, Vector2.zero) };
                UpdateLine(link, data.from.MapPosition, data.to.MapPosition, data.available ? Cyan : Gold);
                link.root.gameObject.SetActive(true);
            }

            bool showCaption = visible > 0 && progress.HasFlag(VisitedFlag);
            captionRoot.gameObject.SetActive(showCaption);
            if (showCaption)
            {
                if (game.Current == null || game.Current.id == "D17Z01S05") labelCell = entranceCell ?? labelCell;
                captionRoot.anchoredPosition = labelCell.MapPosition + Vector2.up * 34;
                caption.text = "Hầm thử thách · " + Mathf.Clamp(route.CompletedObjectives, 0, 3) + "/3";
            }
            gameObject.SetActive(visible > 0);
        }

        static void AddCell(Dictionary<string, CellView> cells, SourceMap.Cell cell, bool available)
        {
            if (cell == null || cell.ngPlus) return;
            if (!cells.TryGetValue(cell.key, out var view)) cells[cell.key] = view = new CellView { cell = cell };
            view.available |= available;
        }

        void EnsureUI()
        {
            if (pathsRoot != null && markersRoot != null && captionRoot != null && caption != null) return;
            if (pathsRoot == null) links.Clear();
            if (markersRoot == null) markers.Clear();
            pathsRoot = MakeRect(transform, "PassageLinks", Vector2.zero, Vector2.zero);
            markersRoot = MakeRect(transform, "TunnelMarkers", Vector2.zero, Vector2.zero);
            captionRoot = MakeRect(transform, "TrialProgress", Vector2.zero, new Vector2(194, 26));
            AddImage(captionRoot, new Color(.025f, .025f, .04f, .9f));
            var textRect = MakeRect(captionRoot, "Label", Vector2.zero, new Vector2(190, 24));
            caption = textRect.GetComponent<Text>();
            if (caption == null) caption = textRect.gameObject.AddComponent<Text>();
            labelFont = VietnameseSource.DynamicFont;
            if (labelFont == null) labelFont = Resources.Load<Font>("Fonts/Caudex-Regular");
            if (labelFont == null) labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            caption.font = labelFont;
            caption.fontSize = 14;
            caption.alignment = TextAnchor.MiddleCenter;
            caption.color = Gold;
            caption.raycastTarget = false;
            caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            caption.verticalOverflow = VerticalWrapMode.Overflow;
        }

        MarkerView CreateMarker(string key)
        {
            var root = MakeRect(markersRoot, "Tunnel_" + key, Vector2.zero, new Vector2(16, 16));
            AddImage(root, new Color(.025f, .025f, .04f, .96f));
            var edges = new Image[3];
            edges[0] = AddImage(MakeRect(root, "LeftPost", new Vector2(-5, -1), new Vector2(2, 10)), Gold);
            edges[1] = AddImage(MakeRect(root, "RightPost", new Vector2(5, -1), new Vector2(2, 10)), Gold);
            edges[2] = AddImage(MakeRect(root, "Lintel", new Vector2(0, 5), new Vector2(12, 2)), Gold);
            return new MarkerView { root = root, edges = edges };
        }

        static bool ValidMarker(MarkerView marker)
        {
            if (marker.root == null || marker.edges == null || marker.edges.Length != 3) return false;
            foreach (var edge in marker.edges) if (edge == null) return false;
            return true;
        }

        static void UpdateLine(LinkView link, Vector2 from, Vector2 to, Color color)
        {
            for (int i = 0; i < link.dashes.Count; i++)
                if (link.dashes[i] == null) { link.dashes.Clear(); link.hasGeometry = false; break; }
            if (!link.hasGeometry || link.from != from || link.to != to)
            {
                Vector2 delta = to - from;
                float distance = delta.magnitude;
                Vector2 direction = distance > .001f ? delta / distance : Vector2.right;
                const float dashLength = 7, spacing = 12;
                int count = Mathf.CeilToInt(distance / spacing);
                while (link.dashes.Count < count)
                    link.dashes.Add(AddImage(MakeRect(link.root, "Dash_" + link.dashes.Count, Vector2.zero, Vector2.zero), color));
                for (int i = 0; i < link.dashes.Count; i++)
                {
                    var dash = link.dashes[i];
                    dash.gameObject.SetActive(i < count);
                    if (i >= count) continue;
                    float start = i * spacing, length = Mathf.Min(dashLength, distance - start);
                    dash.rectTransform.anchoredPosition = from + direction * (start + length * .5f);
                    dash.rectTransform.sizeDelta = new Vector2(length, 2.5f);
                    dash.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                }
                link.from = from;
                link.to = to;
                link.hasGeometry = true;
            }
            foreach (var dash in link.dashes) dash.color = color;
        }

        static RectTransform MakeRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var existing = parent.Find(name);
            var rect = existing != null ? existing.GetComponent<RectTransform>() : null;
            if (rect == null) rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
