using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemigo_BT))]
public class BehaviorTreeInspector : Editor
{
    // Colores de estado
    static readonly Color ColorRunning = new Color(1.00f, 0.75f, 0.00f); // ámbar
    static readonly Color ColorSuccess = new Color(0.20f, 0.80f, 0.20f); // verde
    static readonly Color ColorFailure = new Color(0.85f, 0.20f, 0.20f); // rojo
    static readonly Color ColorNone    = new Color(0.50f, 0.50f, 0.50f); // gris

    const float ROW_HEIGHT   = 22f;
    const float INDENT_WIDTH = 20f;

    void OnEnable()  => EditorApplication.update += Repaint;
    void OnDisable() => EditorApplication.update -= Repaint;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var bt = (Enemigo_BT)target;
        if (!Application.isPlaying || bt.Tree == null)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("Entra en Play Mode para ver el árbol en tiempo real.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Behavior Tree — Estado en tiempo real", EditorStyles.boldLabel);
        DrawLegend();
        EditorGUILayout.Space(4);

        DrawNode(bt.Tree, 0);
    }

    // ── Leyenda de colores ────────────────────────────────────────────────

    void DrawLegend()
    {
        EditorGUILayout.BeginHorizontal();
        DrawColorChip(ColorRunning); EditorGUILayout.LabelField("Running", GUILayout.Width(62));
        DrawColorChip(ColorSuccess); EditorGUILayout.LabelField("Success", GUILayout.Width(62));
        DrawColorChip(ColorFailure); EditorGUILayout.LabelField("Failure", GUILayout.Width(62));
        DrawColorChip(ColorNone);    EditorGUILayout.LabelField("Sin evaluar", GUILayout.Width(72));
        EditorGUILayout.EndHorizontal();
    }

    void DrawColorChip(Color c)
    {
        var rect = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14), GUILayout.Height(14));
        rect.y += 2f;
        EditorGUI.DrawRect(rect, c);
    }

    // ── Dibuja un nodo y sus hijos recursivamente ─────────────────────────

    void DrawNode(BTNode node, int depth)
    {
        if (node == null) return;

        Color statusColor = GetStatusColor(node.LastStatus);

        var rowRect = EditorGUILayout.GetControlRect(false, ROW_HEIGHT);
        rowRect.xMin += depth * INDENT_WIDTH;

        // Fondo translúcido
        EditorGUI.DrawRect(rowRect, new Color(statusColor.r, statusColor.g, statusColor.b, 0.18f));

        // Franja izquierda sólida
        EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, 3f, rowRect.height), statusColor);

        // Área de texto (inset)
        var content = new Rect(rowRect.x + 7f, rowRect.y + 2f, rowRect.width - 7f, rowRect.height - 4f);

        // Tipo del nodo
        const float TYPE_W = 60f;
        EditorGUI.LabelField(
            new Rect(content.x, content.y, TYPE_W, content.height),
            GetNodeTypeTag(node),
            EditorStyles.miniLabel
        );

        // Nombre del nodo
        const float STATUS_W = 72f;
        float nameW = content.width - TYPE_W - STATUS_W;
        EditorGUI.LabelField(
            new Rect(content.x + TYPE_W, content.y, nameW, content.height),
            GetNodeDisplayName(node),
            EditorStyles.boldLabel
        );

        // Estado alineado a la derecha
        string statusText = node.LastStatus.HasValue ? node.LastStatus.Value.ToString() : "—";
        var rightStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleRight,
            normal    = { textColor = statusColor }
        };
        EditorGUI.LabelField(
            new Rect(content.xMax - STATUS_W, content.y, STATUS_W, content.height),
            statusText,
            rightStyle
        );

        // Recursión
        if (node is Selector sel)
            foreach (var child in sel.Children) DrawNode(child, depth + 1);
        else if (node is Sequence seq)
            foreach (var child in seq.Children) DrawNode(child, depth + 1);
        else if (node is Inverter inv)
            DrawNode(inv.Child, depth + 1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static Color GetStatusColor(NodeStatus? status)
    {
        if (!status.HasValue) return ColorNone;
        switch (status.Value)
        {
            case NodeStatus.Running: return ColorRunning;
            case NodeStatus.Success: return ColorSuccess;
            case NodeStatus.Failure: return ColorFailure;
            default:                 return ColorNone;
        }
    }

    static string GetNodeTypeTag(BTNode node)
    {
        if (node is Selector)  return "[SEL]";
        if (node is Sequence)  return "[SEQ]";
        if (node is Inverter)  return "[INV]";
        if (node is Condition) return "[COND]";
        if (node is BTAction)  return "[ACT]";
        return $"[{node.GetType().Name}]";
    }

    static string GetNodeDisplayName(BTNode node)
    {
        if (!string.IsNullOrEmpty(node.Name)) return node.Name;
        return node.GetType().Name;
    }
}
