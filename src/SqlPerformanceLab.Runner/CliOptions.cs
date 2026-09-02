namespace SqlPerformanceLab.Runner;

internal sealed record CliOptions(
    string Command,
    string? Scenario,
    int Iterations,
    string? ConnectionString,
    string? OutputPath)
{
    public static CliOptions Parse(string[] args)
    {
        if (args.Length == 0)
            return new CliOptions("help", null, 3, null, null);

        string command = args[0].Trim().ToLowerInvariant();
        string? scenario = null;
        string? connection = null;
        string? output = null;
        int iterations = 3;

        for (int i = 1; i < args.Length; i++)
        {
            string arg = args[i];

            switch (arg)
            {
                case "--scenario":
                    scenario = RequireValue(args, ref i, arg);
                    break;

                case "--iterations":
                    string rawIterations = RequireValue(args, ref i, arg);

                    if (!int.TryParse(rawIterations, out iterations) || iterations is < 1 or > 20)
                        throw new ArgumentException("--iterations must be between 1 and 20.");

                    break;

                case "--connection":
                    connection = RequireValue(args, ref i, arg);
                    break;

                case "--output":
                    output = RequireValue(args, ref i, arg);
                    break;

                case "--help":
                case "-h":
                    return new CliOptions("help", null, 3, null, null);

                default:
                    throw new ArgumentException($"Unknown option '{arg}'.");
            }
        }

        return new CliOptions(command, scenario, iterations, connection, output);
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
            throw new ArgumentException($"Missing value for {option}.");

        return args[++index];
    }
}
