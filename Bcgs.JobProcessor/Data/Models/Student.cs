using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bcgs.JobProcessor.Data.Models
{
    public class Student
    {
        [Key]
        public int id { get; set; }

        public int admission_no { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string is_active { get; set; }
        public string guardian_name { get; set; }
        public string guardian_phone { get; set; }
        public ICollection<StudentSession> StudentSessions { get; set; }
        /*
         id
        parent_id
        admission_no
        roll_no
        admission_date
        firstname
        lastname
        rte
        image
        mobileno
        email
        state
        city
        pincode
        religion
        cast
        dob
        gender
        current_address
        permanent_address
        category_id
        route_id
        school_house_id
        blood_group
        vehroute_id
        hostel_room_id
        adhar_no
        samagra_id
        bank_account_no
        bank_name
        ifsc_code
        guardian_is
        father_name
        father_phone
        father_occupation
        mother_name
        mother_phone
        mother_occupation
        guardian_name
        guardian_relation
        guardian_phone
        guardian_occupation
        guardian_address
        guardian_email
        father_pic
        mother_pic
        guardian_pic
        is_active
        previous_school
        height
        weight
        measurement_date
        app_key
        parent_app_key
        created_at
        updated_at
        disable_at
        note
                 */
    }
}
