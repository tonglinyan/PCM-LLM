using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaledAnimationCurve : AnimationCurve
{
    protected Vector2 m_scale;
    protected Vector2 m_offset;

    public ScaledAnimationCurve(Vector2 scale) : base()
    {
        Initialize(scale, Vector2.zero);
    }

    public ScaledAnimationCurve(Vector2 scale, Vector2 offset): base()
    {
        Initialize(scale, offset);
    }

    public ScaledAnimationCurve(Vector2 scale, params Keyframe[] keys) : base(keys)
    {
        Initialize(scale, Vector2.zero);
    }

    public ScaledAnimationCurve(Vector2 scale, Vector2 offset, params Keyframe[] keys) : base(keys)
    {
        Initialize(scale, offset);
    }

    private void Initialize(Vector2 scale, Vector2 offset)
    {
        m_scale = scale;
        m_offset = offset;
    }

    public new float Evaluate(float time)
    {
        time = (time - TimeOffset) / TimeScale;
        float value = base.Evaluate(time);
        value = value * ValueScale + ValueOffset;
        return value;
    }

    public float TimeScale
    {
        get { return m_scale.x; }
    }
    public float TimeOffset
    {
        get { return m_offset.x; }
    }
    public float ValueScale
    {
        get { return m_scale.y; }
    }
    public float ValueOffset
    {
        get { return m_offset.y; }
    }
}
