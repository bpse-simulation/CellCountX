using CellCountX.Wpf.Model;

namespace CellCountX.Wpf.Logic;

public class PythonClient
{
    private readonly PythonServer _server;

    public PythonClient(PythonServer server)
    {
        _server = server;
    }

    public async Task<PythonResponse> RunAsync(string json, int timeoutSeconds)
    {
        try
        {
            // PythonServerResult を受け取る
            var result = await _server.RunOnceAsync(json, timeoutSeconds);

            if (result.IsError)
            {
                return new PythonResponse
                {
                    IsError = true,
                    ErrorMessage = result.ErrorMessage
                };
            }

            return new PythonResponse
            {
                IsError = false,
                RawOutput = result.Output
            };
        }
        catch (Exception ex)
        {
            return new PythonResponse
            {
                IsError = true,
                ErrorMessage = ex.Message
            };
        }
    }
}
