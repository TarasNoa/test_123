import { GrpcWebClient } from "grpc-web";
import { SandboxServiceClient } from "../generated/sandbox_pb";

// gRPC-web client for communicating with Rust sandbox
export class GrpcSandboxClient {
  private client: SandboxServiceClient;

  constructor(endpoint: string = "http://localhost:50051") {
    const grpcClient = new GrpcWebClient({
      format: "text",
    });

    this.client = new SandboxServiceClient(endpoint, null, grpcClient);
  }

  async executeCode(request: {
    taskId: string;
    code: string;
    language: string;
    memoryLimitMb: number;
    timeoutSeconds: number;
  }) {
    const req = new ExecutionRequest();
    req.setTaskId(request.taskId);
    req.setCode(request.code);
    req.setLanguage(request.language);
    req.setMemoryLimitMb(request.memoryLimitMb);
    req.setTimeoutSeconds(request.timeoutSeconds);

    return new Promise((resolve, reject) => {
      this.client.executeCode(req, {}, (err, response) => {
        if (err) {
          reject(err);
        } else {
          resolve({
            stdout: response?.getStdout() || "",
            stderr: response?.getStderr() || "",
            exitCode: response?.getExitCode() || -1,
            terminationReason: response?.getTerminationReason() || "Unknown",
            resources: response?.getResources()?.toObject(),
          });
        }
      });
    });
  }
}

export const grpcClient = new GrpcSandboxClient();
