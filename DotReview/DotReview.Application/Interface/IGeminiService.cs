using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotReview.Application.Interface
{
    public interface IGeminiService
    {
        Task<string> ReviewCodeAsync(
            string language,
            string code);
    }
}
