using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_access_layer.model
{
    public class assignment_Answer
    {
        [Key]
        public int ID { get; set; }
        [ForeignKey(nameof(Student))]
        public int StudentID { get; set; }
        public string Answer { get; set; }
        [ForeignKey(nameof(assignment_question))]
        public int assignment_question_id { get; set; }
        public virtual assignment_question assignment_question { get; set; }
        public virtual Student Student { get; set; }
    }
}
