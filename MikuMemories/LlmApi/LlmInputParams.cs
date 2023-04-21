using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikuMemories
{
    /*
* 'max_new_tokens': 200,
'do_sample': True,
'temperature': 0.72,
'top_p': 0.73,
'typical_p': 1,
'repetition_penalty': 1.1,
'encoder_repetition_penalty': 1.0,
'top_k': 0,
'min_length': 0,
'no_repeat_ngram_size': 0,
'num_beams': 1,
'penalty_alpha': 0,
'length_penalty': 1,
'early_stopping': False,
'seed': -1,
'add_bos_token': True,
'truncation_length': 2048,
'ban_eos_token': False,
'skip_special_tokens': True,
'stopping_strings': [],
*/

    public class LlmInputParams
    {
        public int max_new_tokens { get; set; }
        public bool do_sample { get; set; }
        public float temperature { get; set; }
        public float top_p { get; set; }
        public float typical_p { get; set; }
        public float repetition_penalty { get; set; }
        public float encoder_repetition_penalty { get; set; }
        public int top_k { get; set; }
        public int min_length { get; set; }
        public int no_repeat_ngram_size { get; set; }
        public int num_beams { get; set; }
        public float penalty_alpha { get; set; }
        public float length_penalty { get; set; }
        public bool early_stopping { get; set; }
        public int seed { get; set; }
        public bool add_bos_token { get; set; }
        public int truncation_length { get; set; }
        public bool ban_eos_token { get; set; }
        public bool skip_special_tokens { get; set; }
        public string[] stopping_strings { get; set; }


        public LlmInputParams(int max_new_tokens, bool do_sample, float temperature, float top_p, float typical_p, float repetition_penalty, float encoder_repetition_penalty, int top_k, int min_length, int no_repeat_ngram_size, int num_beams, float penalty_alpha, float length_penalty, bool early_stopping, int seed, bool add_bos_token, int truncation_length, bool ban_eos_token, bool skip_special_tokens, string[] stopping_strings)
        {
            this.max_new_tokens = max_new_tokens;
            this.do_sample = do_sample;
            this.temperature = temperature;
            this.top_p = top_p;
            this.typical_p = typical_p;
            this.repetition_penalty = repetition_penalty;
            this.encoder_repetition_penalty = encoder_repetition_penalty;
            this.top_k = top_k;
            this.min_length = min_length;
            this.no_repeat_ngram_size = no_repeat_ngram_size;
            this.num_beams = num_beams;
            this.penalty_alpha = penalty_alpha;
            this.length_penalty = length_penalty;
            this.early_stopping = early_stopping;
            this.seed = seed;
            this.add_bos_token = add_bos_token;
            this.truncation_length = truncation_length;
            this.ban_eos_token = ban_eos_token;
            this.skip_special_tokens = skip_special_tokens;
            this.stopping_strings = stopping_strings;
        }

        public static LlmInputParams defaultParams
            = new LlmInputParams(
                200,
                true,
                0.72f,
                0.73f,
                1,
                1.1f,
                1.0f,
                0,
                0,
                0,
                1,
                0,
                1,
                false,
                -1,
                true,
                2048,
                false,
                true,
                new string[0]
                );
    }

    /*
* 'max_new_tokens': 200,
'do_sample': True,
'temperature': 0.72,
'top_p': 0.73,
'typical_p': 1,
'repetition_penalty': 1.1,
'encoder_repetition_penalty': 1.0,
'top_k': 0,
'min_length': 0,
'no_repeat_ngram_size': 0,
'num_beams': 1,
'penalty_alpha': 0,
'length_penalty': 1,
'early_stopping': False,
'seed': -1,
'add_bos_token': True,
'truncation_length': 2048,
'ban_eos_token': False,
'skip_special_tokens': True,
'stopping_strings': [],
*/
}
