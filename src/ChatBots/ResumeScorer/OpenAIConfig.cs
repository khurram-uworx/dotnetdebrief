namespace ChatBots.ResumeScorer;

//sealed record OpenAIConfig(string BaseUrl, string ApiKey, string Model);

//static class OpenAIConfigLoader
//{
//    public static OpenAIConfig Load()
//    {
//        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
//        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
//        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL");

//        var missing = new List<string>();
//        if (string.IsNullOrWhiteSpace(baseUrl)) missing.Add("OPENAI_BASE_URL");
//        if (string.IsNullOrWhiteSpace(apiKey)) missing.Add("OPENAI_API_KEY");
//        if (string.IsNullOrWhiteSpace(model)) missing.Add("OPENAI_MODEL");

//        if (missing.Count != 0)
//            throw new InvalidOperationException(
//                "Missing required environment variable(s): " + string.Join(", ", missing) +
//                ". Set OPENAI_BASE_URL, OPENAI_API_KEY and OPENAI_MODEL before running.");
//        return new OpenAIConfig(baseUrl!, apiKey!, model!);
//    }
//}
