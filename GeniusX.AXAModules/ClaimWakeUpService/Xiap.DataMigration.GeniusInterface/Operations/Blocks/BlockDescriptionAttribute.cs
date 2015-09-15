namespace Xiap.DataMigration.GeniusInterface.AXACS.Operations.Blocks
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class BlockDescriptionAttribute : Attribute
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public bool Default { get; set; }

        public BlockDescriptionAttribute(string name)
        {
            Name = name;
            Default = true;
        }
    }
}
