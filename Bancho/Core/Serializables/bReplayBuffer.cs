using System;
using System.Collections.Generic;
using osuBancho.Helpers;

namespace osuBancho.Core.Serializables
{
    [Flags]
    public enum pButtonState
    {
        None = 0,
        Left1 = 1,
        Right1 = 2,
        Left2 = 4,
        Right2 = 8,
        Smoke = 16
    }

    internal sealed class ReplayFrame : bSerializable
    {
        public bool bool_0;
        public bool bool_1;
        public bool bool_2;
        public bool bool_3;
        public bool bool_4;
        public bool bool_5;
        public pButtonState MouseButtonState;
        public float MousePosX;
        public float MousePosY;
        public int Time;

        public ReplayFrame(int time, float mousePosX, float posY, pButtonState mouseButtonState)
        {
            MousePosX = mousePosX;
            MousePosY = posY;
            MouseButtonState = mouseButtonState;
            method_0(mouseButtonState);
            this.Time = time;
        }

        public ReplayFrame(SerializationReader reader)
        {
            MouseButtonState = (pButtonState) reader.ReadByte();
            method_0(MouseButtonState);
            var b = reader.ReadByte();
            if (b > 0)
            {
                method_0(pButtonState.Right1);
            }
            MousePosX = reader.ReadSingle();
            MousePosY = reader.ReadSingle();
            Time = reader.ReadInt32();
        }

        public void ReadFromStream(SerializationReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteToStream(SerializationWriter writer)
        {
            writer.Write((byte) MouseButtonState);
            writer.Write((byte) 0);
            writer.Write(MousePosX);
            writer.Write(MousePosY);
            writer.Write(Time);
        }

        public void method_0(pButtonState pButtonState_1)
        {
            MouseButtonState = pButtonState_1;
            bool_0 = ((pButtonState_1 & (pButtonState.Left1 | pButtonState.Left2)) > pButtonState.None);
            bool_2 = ((pButtonState_1 & pButtonState.Left1) > pButtonState.None);
            bool_4 = ((pButtonState_1 & pButtonState.Left2) > pButtonState.None);
            bool_1 = ((pButtonState_1 & (pButtonState.Right1 | pButtonState.Right2)) > pButtonState.None);
            bool_3 = ((pButtonState_1 & pButtonState.Right1) > pButtonState.None);
            bool_5 = ((pButtonState_1 & pButtonState.Right2) > pButtonState.None);
        }
    }

    internal struct bScoreData : bSerializable
    {
        public bool bool_0;
        public bool bool_1;
        public byte byte_0;
        public int int_0;
        public int int_1;
        public int int_2;
        public int int_3;
        public ushort ushort_0;
        public ushort ushort_1;
        public ushort ushort_2;
        public ushort ushort_3;
        public ushort ushort_4;
        public ushort ushort_5;
        public ushort ushort_6;
        public ushort ushort_7;

        public bScoreData(SerializationReader reader)
        {
            int_1 = reader.ReadInt32();
            byte_0 = reader.ReadByte();
            ushort_1 = reader.ReadUInt16();
            ushort_0 = reader.ReadUInt16();
            ushort_2 = reader.ReadUInt16();
            ushort_3 = reader.ReadUInt16();
            ushort_4 = reader.ReadUInt16();
            ushort_5 = reader.ReadUInt16();
            int_2 = reader.ReadInt32();
            ushort_7 = reader.ReadUInt16();
            ushort_6 = reader.ReadUInt16();
            bool_1 = reader.ReadBoolean();
            int_0 = reader.ReadByte();
            int_3 = reader.ReadByte();
            if (int_0 == 254)
            {
                int_0 = 0;
                bool_0 = false;
                return;
            }
            bool_0 = true;
        }

        public void ReadFromStream(SerializationReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteToStream(SerializationWriter writer)
        {
            writer.Write(int_1);
            writer.Write(byte_0);
            writer.Write(ushort_1);
            writer.Write(ushort_0);
            writer.Write(ushort_2);
            writer.Write(ushort_3);
            writer.Write(ushort_4);
            writer.Write(ushort_5);
            writer.Write(int_2);
            writer.Write(ushort_7);
            writer.Write(ushort_6);
            writer.Write(bool_1);
            writer.Write((byte) (bool_0 ? int_0 : 254));
            writer.Write(int_3);
        }
    }

    internal enum Enum0
    {
        PlayData,
        Start,
        const_2,
        const_3,
        const_4,
        Pause,
        const_6,
        const_7,
        const_8
    }

    internal sealed class bReplayBuffer : bSerializable
    {
        public bScoreData BScoreData0;
        public Enum0 enum0_0;
        public int int_0;
        public List<ReplayFrame> replayFrames;

        public bReplayBuffer(List<ReplayFrame> list_1, Enum0 enum0_1, bScoreData bScoreData1, int int_1)
        {
            replayFrames = list_1;
            enum0_0 = enum0_1;
            BScoreData0 = bScoreData1;
            int_0 = int_1;
        }

        public bReplayBuffer(SerializationReader reader)
        {
            if (Bancho.Protocol >= 18)
            {
                int_0 = reader.ReadInt32();
            }
            replayFrames = new List<ReplayFrame>();
            int num = reader.ReadUInt16();
            for (var i = 0; i < num; i++)
            {
                replayFrames.Add(new ReplayFrame(reader));
            }
            enum0_0 = (Enum0) reader.ReadByte();
            try
            {
                BScoreData0 = new bScoreData(reader);
            }
            catch (Exception)
            {
            }
        }

        public void ReadFromStream(SerializationReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteToStream(SerializationWriter writer)
        {
            if (Bancho.Protocol >= 18)
            {
                writer.Write(int_0);
            }
            writer.Write((ushort) replayFrames.Count);
            foreach (var current in replayFrames)
            {
                current.WriteToStream(writer);
            }
            writer.Write((byte) enum0_0);
            BScoreData0.WriteToStream(writer);
        }
    }
}