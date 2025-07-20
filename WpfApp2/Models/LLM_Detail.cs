using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WpfApp2.Models
{
    public class LLM_Detail
    {
        [Key]
        public int Id { get; set; }
        [Required]

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public SignUpDetail SignUpDetail { get; set; } = null!;
         
        public string? Expected_json { get; set; }=null!;

        public string? user_command { get; set; } = null;

        public string? Status { get; set; } = null;

        public DateTime? CommandTime { get; set; }=DateTime.Now;

        public string? CommandType { get; set; } = null;

        public Brush StatusColor => Status == "Success" ? Brushes.Green : Brushes.Red;

    }
}
