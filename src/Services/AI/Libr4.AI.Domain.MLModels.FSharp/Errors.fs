namespace Libr4.AI.Domain.MLModels.FSharp

module MLModelErrors =
    type MLModelError =
        | DeploymentFailed
        | ValidationError
        | InsufficientData
        | ModelNotFound
        | ExperimentFailed
        | ABTestNotReady

    let errorMessage = function
        | DeploymentFailed -> "Deployment failed"
        | ValidationError -> "Validation error"
        | InsufficientData -> "Insufficient training data"
        | ModelNotFound -> "Model not found"
        | ExperimentFailed -> "Experiment failed"
        | ABTestNotReady -> "A/B test not ready"

    type ValidationResult<'T> = Result<'T, MLModelError>
