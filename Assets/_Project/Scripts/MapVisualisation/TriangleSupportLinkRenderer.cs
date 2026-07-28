using System.Collections.Generic;
using UnityEngine;

public class TriangleSupportLinkRenderer : MonoBehaviour
{
    [Header("Line Visual")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color supportLinkColor = new Color(1f, 1f, 0.15f, 1f);
    [SerializeField] private float lineWidth = 0.04f;
    [SerializeField] private float zOffset = -0.08f;

    private readonly List<LineRenderer> activeLines = new();

    public void Clear()
    {
        for (int i = 0; i < activeLines.Count; i++)
        {
            if (activeLines[i] != null)
                activeLines[i].gameObject.SetActive(false);
        }
    }

    public void ShowLinks(IEnumerable<UnitSupportLink> links)
    {
        Clear();

        if (links == null)
            return;

        int index = 0;

        foreach (UnitSupportLink link in links)
        {
            if (link == null || link.supporter == null || link.receiver == null)
                continue;

            LineRenderer line = GetLine(index);
            index++;

            Vector3 start = GetUnitCenter(link.supporter);
            Vector3 end = GetUnitCenter(link.receiver);

            start.z += zOffset;
            end.z += zOffset;

            line.gameObject.SetActive(true);
            line.positionCount = 2;
            line.useWorldSpace = true;

            line.SetPosition(0, start);
            line.SetPosition(1, end);

            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.startColor = supportLinkColor;
            line.endColor = supportLinkColor;
        }
    }

    private LineRenderer GetLine(int index)
    {
        while (activeLines.Count <= index)
        {
            GameObject lineObject = new GameObject($"Support Link {activeLines.Count}");
            lineObject.transform.SetParent(transform, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.sortingOrder = 200;

            activeLines.Add(line);
        }

        return activeLines[index];
    }

    private Vector3 GetUnitCenter(UnitPiece unit)
    {
        if (unit == null || unit.OccupiedCells == null || unit.OccupiedCells.Count == 0)
            return unit != null ? unit.transform.position : Vector3.zero;

        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (TriangleCell cell in unit.OccupiedCells)
        {
            if (cell == null)
                continue;

            sum += cell.CenterPosition;
            count++;
        }

        if (count == 0)
            return unit.transform.position;

        return sum / count;
    }
}