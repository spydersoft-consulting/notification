using Spydersoft.Notification.TokenGenerator;

// First positional arg that isn't a flag is the user id; flags start with "--".
var userIdArg = Array.Find(args, a => !a.StartsWith("--", StringComparison.Ordinal));
var userId = userIdArg ?? TokenGenerator.DefaultTestUserId;

var testKey = Environment.GetEnvironmentVariable("NOTIFICATION_TEST_KEY")
    ?? "jRv3YFPH/19t9t5CgsEFgAkykfW5bQhHmceMprLgzlQ=";

var machine = args.Contains("--machine");
var readOnly = args.Contains("--read-only");

Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
    new { token = TokenGenerator.Generate(testKey, userId, machine, readOnly) }));
