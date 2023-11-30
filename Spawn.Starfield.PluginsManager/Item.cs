using System.Diagnostics;

namespace Spawn.Starfield.PluginsManager
{
    [DebuggerDisplay("{Name} -  LoadIndex={LoadIndex} - IsActive={IsActive}")]
    public class Item(string name, bool isActive)
    {
        public string Name { get; } = name;
        public bool IsActive { get; set; } = isActive;
        public int LoadIndex { get; set; } = -1;
    }
}