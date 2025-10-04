namespace ModdingTool.Assets.Editor.SpritesheetFromDumps
{
    public readonly struct RenderDataKey
    {
        public readonly uint firstData0;
        public readonly uint firstData1;
        public readonly uint firstData2;
        public readonly uint firstData3;
        public readonly long second;

        public RenderDataKey(uint firstData0, uint firstData1, uint firstData2, uint firstData3, long second)
        {
            this.firstData0 = firstData0;
            this.firstData1 = firstData1;
            this.firstData2 = firstData2;
            this.firstData3 = firstData3;
            this.second = second;
        }

        public bool Equals(RenderDataKey other)
        {
            return firstData0 == other.firstData0 &&
                firstData1 == other.firstData1 &&
                firstData2 == other.firstData2 &&
                firstData3 == other.firstData3 &&
                second == other.second;
        }

        public override string ToString()
        {
            return $"\"first\"[{firstData0}, {firstData1}, {firstData2}, {firstData3}], \"second\": {second}";
        }
    }
}