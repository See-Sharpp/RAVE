using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Models
{
    public class AllDocs
    {
        [Key]
        public int Id { get; set; }


        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]

        public SignUpDetail SignUpDetail { get; set; } = null!;

        public string? FileName { get; set; }

        public string? FilePath { get; set; }

        public string? DisplayName { get; set; }

        public string? FileSize { get; set; }

        public DateTime LastWriteTime { get; set; }

        public DateTime LastAccessTime { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? Embedding { get; set; }
    }
}
