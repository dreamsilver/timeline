﻿using System.IO;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;

public class AnchorAsset : PlayableAsset, IDirectorIO
{
    const int CURVE_CNT = 3;
    [SerializeField] AnimationCurve[] m_Clip_Pos;
    [SerializeField] AnimationCurve[] m_Clip_Rot;
    [SerializeField] TrackAsset m_Track;

    public AnimationCurve[] clip_pos
    {
        get { return m_Clip_Pos; }
        set { m_Clip_Pos = value; }
    }


    public AnimationCurve[] clip_rot
    {
        get { return m_Clip_Rot; }
        set { m_Clip_Rot = value; }
    }

    public TrackAsset track
    {
        get { return m_Track; }
        set { m_Track = value; }
    }

    public bool IsValid()
    {
        return m_Track != null &&
            m_Clip_Pos != null &&
            m_Clip_Rot != null;
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var director = owner.GetComponent<PlayableDirector>();
        Transform tf = ExternalHelp.FetchAttachOfTrack(director, track);
        AnchorBehaviour beha = new AnchorBehaviour();
        beha.Set(clip_pos, clip_rot, tf, director);
        return ScriptPlayable<AnchorBehaviour>.Create(graph, beha);
    }

    public void Load(BinaryReader reader)
    {
        int keyCount = reader.ReadInt32();
        if (keyCount > 0)
        {
            m_Clip_Pos = new AnimationCurve[CURVE_CNT];
            m_Clip_Rot = new AnimationCurve[CURVE_CNT];
            for (int i = 0; i < CURVE_CNT; i++)
            {
                m_Clip_Pos[i] = new AnimationCurve();
                ReadCurve(reader, keyCount, (t, v) => m_Clip_Pos[i].AddKey(t, v));
            }
            for (int i = 0; i < CURVE_CNT; i++)
            {
                m_Clip_Rot[i] = new AnimationCurve();
                ReadCurve(reader, keyCount, (t, v) => m_Clip_Rot[i].AddKey(t, v));
            }
        }
    }

    private void ReadCurve(BinaryReader reader, int cnt, Action<float, float> cb)
    {
        for (int i = 0; i < cnt; i++)
        {
            float t = reader.ReadSingle();
            float v = reader.ReadSingle();
            cb(t, v);
        }
    }


    public void Write(BinaryWriter writer)
    {
        bool hasV = m_Clip_Pos != null;
        int keyCount = hasV ? m_Clip_Pos[0].keys.Length : 0;
        writer.Write(keyCount);
        if (hasV)
        {
            for (int i = 0; i < CURVE_CNT; i++)
            {
                Sample(m_Clip_Pos[i], (t, v) =>
                 {
                     writer.Write(t);
                     writer.Write(v);
                 });
                Sample(m_Clip_Rot[i], (t, v) =>
                {
                    writer.Write(t);
                    writer.Write(v);
                });
            }
        }
    }

    private void Sample(AnimationCurve curve, Action<float, float> cb)
    {
        int len = curve.keys.Length;
        if (cb != null)
        {
            for (int i = 0; i < len; i++)
            {
                float t = curve.keys[i].time;
                float v = curve.keys[i].value;
                cb(t, v);
            }
        }
    }

}