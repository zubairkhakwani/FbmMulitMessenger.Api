using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class LocalServer
    {
        public int Id { get; set; }

        public string UniqueId { get; set; } = string.Empty;

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        //BASIC INFO    
        public double TotalMemoryGB { get; set; }                    // RAM size
        public int LogicalProcessors { get; set; }                   // Total logical processors
        public string ProcessorName { get; set; } = string.Empty;    // CPU model name
        public int CoreCount { get; set; }                           // Physical cores

        //CPU STRENGTH PROPERTIES
        public int MaxClockSpeedMHz { get; set; }                    // Max CPU speed (turbo boost)
        public int CurrentClockSpeedMHz { get; set; }                // Current CPU speed
        public string CpuArchitecture { get; set; } = string.Empty;  // x64, ARM64, etc.
        public int CpuThreadCount { get; set; }                      // Total threads (with hyperthreading)

        //GPU STRENGTH
        public string GraphicsCards { get; set; } = string.Empty;   // GPU name(s) and VRAM

        //STORAGE STRENGTH
        public double TotalStorageGB { get; set; }                   // Total disk space
        public bool HasSSD { get; set; }                             // SSD vs HDD indicator

        // OS INFO
        public string OperatingSystem { get; set; } = string.Empty;  // Windows version
        public bool Is64BitOS { get; set; }                          // 64-bit vs 32-bit

        // UNIQUE IDENTIFIERS 
        public string SystemUUID { get; set; } = string.Empty;       // Mobo factory ID (best)
        public string MotherboardSerial { get; set; } = string.Empty; // Mobo serial number
        public string ProcessorId { get; set; } = string.Empty;      // CPU chip ID

        public int MaxBrowserCapacity { get; set; } // Maximum browsers this system can handle
        public int ActiveBrowserCount { get; set; } // Currently running browser count
        public bool IsActive { get; set; }
        public DateTime RegisteredAt { get; set; }


        //Navigation Properties

        public User User { get; set; } = null!;
        public List<Account> Accounts { get; set; } = new List<Account>();
    }
}
