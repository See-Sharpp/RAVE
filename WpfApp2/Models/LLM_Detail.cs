using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Models
{
    public class LLM_Detail
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("UserId")]
        public SignUpDetail SignUpDetail { get; set; } = null!;
         
        public string? Expected_json { get; set; }=null!;
    }
}
