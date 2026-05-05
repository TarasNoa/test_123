#!/bin/bash
# Build Libr4 development environment Docker images
# Usage: ./build-images.sh [registry] [push] [image]
# Example: ./build-images.sh "ghcr.io/libr4" "true" "all"

set -e

REGISTRY="${1:-}"
PUSH="${2:-false}"
IMAGE="${3:-all}"

# Image configurations
declare -A IMAGES=(
    ["dotnet"]="libr4-env:dotnet|./environments/dotnet|Dockerfile"
    ["python"]="libr4-env:python|./environments/python|Dockerfile"
    ["jvm"]="libr4-env:jvm|./environments/jvm|Dockerfile"
    ["universal"]="libr4-env:universal|./environments/universal|Dockerfile"
)

build_image() {
    local name=$1
    local config=$2
    
    IFS='|' read -r tag path dockerfile <<< "$config"
    
    echo ""
    echo "========================================"
    echo "Building: $name"
    echo "Tag: $tag"
    echo "Path: $path"
    echo "========================================"
    
    local full_tag
    if [ -n "$REGISTRY" ]; then
        full_tag="$REGISTRY/$tag"
    else
        full_tag="$tag"
    fi
    
    local dockerfile_path="$path/$dockerfile"
    
    if docker build -t "$full_tag" -f "$dockerfile_path" "$path"; then
        echo "✅ Successfully built: $full_tag"
        
        if [ "$PUSH" = "true" ] && [ -n "$REGISTRY" ]; then
            echo "Pushing: $full_tag"
            if docker push "$full_tag"; then
                echo "✅ Successfully pushed: $full_tag"
            else
                echo "❌ Failed to push: $full_tag"
                exit 1
            fi
        fi
    else
        echo "❌ Failed to build: $name"
        exit 1
    fi
}

# Main execution
start_time=$(date +%s)

echo "🚀 Starting Libr4 Docker Image Build"
echo "Registry: ${REGISTRY:-local}"
echo "Push: $PUSH"
echo "Images to build: $IMAGE"

if [ "$IMAGE" = "all" ]; then
    for img in "${!IMAGES[@]}"; do
        build_image "$img" "${IMAGES[$img]}"
    done
else
    if [ -n "${IMAGES[$IMAGE]}" ]; then
        build_image "$IMAGE" "${IMAGES[$IMAGE]}"
    else
        echo "❌ Unknown image: $IMAGE"
        exit 1
    fi
fi

end_time=$(date +%s)
duration=$((end_time - start_time))

echo ""
echo "========================================"
echo "Build Complete!"
echo "Duration: $(printf '%02d:%02d:%02d' $((duration/3600)) $((duration%3600/60)) $((duration%60)))"
echo "========================================"
