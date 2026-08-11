namespace ResumeFunctions.Auth.Email
{
    /// <summary>
    /// Minimal <c>{{token}}</c> substitution for building an HTML+plaintext pair from one
    /// template pair and a set of values. Deliberately not a general templating engine — #27
    /// only calls for "just enough for a handful of email types".
    /// </summary>
    public static class EmailTemplates
    {
        public static (string Html, string Text) Render(
            string htmlTemplate,
            string textTemplate,
            IReadOnlyDictionary<string, string> values)
        {
            return (Substitute(htmlTemplate, values), Substitute(textTemplate, values));
        }

        private static string Substitute(string template, IReadOnlyDictionary<string, string> values)
        {
            foreach (var pair in values)
            {
                template = template.Replace("{{" + pair.Key + "}}", pair.Value);
            }

            return template;
        }
    }
}
