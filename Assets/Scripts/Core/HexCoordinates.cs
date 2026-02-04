using UnityEngine;

[System.Serializable]
public struct HexCoordinates
{
    [SerializeField]
    private int q, r;

    public int Q => q;
    public int R => r;
    public int S => -q - r;

    public HexCoordinates(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    public static HexCoordinates FromAxial(int q, int r)
    {
        return new HexCoordinates(q, r);
    }

    public Vector3 ToPosition(float size)
    {
        float x = size * Mathf.Sqrt(3f) * (q + r / 2f);
        float y = size * 1.5f * r;
        return new Vector3(x, y, 0);
    }

    public override string ToString()
    {
        return $"({Q}, {R}, {S})";
    }

    public static int Distance(HexCoordinates a, HexCoordinates b)
    {
        int dQ = Mathf.Abs(a.q - b.q);
        int dR = Mathf.Abs(a.r - b.r);
        int dS = Mathf.Abs(a.S - b.S);
        return (dQ + dR + dS) / 2;
    }
}
