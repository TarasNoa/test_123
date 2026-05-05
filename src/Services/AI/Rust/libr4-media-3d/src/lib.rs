use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum MeshFormat {
    OBJ,
    GLTF,
    GLB,
    STL,
    PLY,
    FBX,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum GenerationModel3D {
    TripoSR,
    ShapE,
    DreamGaussian,
    Wonder3D,
    MVDream,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Vertex {
    pub x: f32,
    pub y: f32,
    pub z: f32,
    pub normal: Option<[f32; 3]>,
    pub uv: Option<[f32; 2]>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Face {
    pub v1: u32,
    pub v2: u32,
    pub v3: u32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Mesh {
    pub id: String,
    pub name: String,
    pub format: MeshFormat,
    pub vertices: Vec<Vertex>,
    pub faces: Vec<Face>,
    pub texture_ids: Vec<String>,
    pub bounding_box: BoundingBox,
    pub file_size: u64,
    pub created_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct BoundingBox {
    pub min: [f32; 3],
    pub max: [f32; 3],
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Texture {
    pub id: String,
    pub image_id: String,
    pub uv_map: String,
    pub resolution: u32,
    pub normal_map: Option<String>,
    pub roughness_map: Option<String>,
    pub metallic_map: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Model3DGeneration {
    pub id: String,
    pub user_id: String,
    pub source_image_id: Option<String>,
    pub prompt: Option<String>,
    pub model: GenerationModel3D,
    pub resolution: u32,
    pub generate_textures: bool,
    pub status: String,
    pub result_mesh_id: Option<String>,
    pub processing_time_ms: u32,
    pub created_at: u64,
}

impl Mesh {
    pub fn vertex_count(&self) -> usize {
        self.vertices.len()
    }

    pub fn face_count(&self) -> usize {
        self.faces.len()
    }

    pub fn triangle_count(&self) -> usize {
        self.faces.len()
    }
}

impl BoundingBox {
    pub fn dimensions(&self) -> [f32; 3] {
        [
            self.max[0] - self.min[0],
            self.max[1] - self.min[1],
            self.max[2] - self.min[2],
        ]
    }

    pub fn center(&self) -> [f32; 3] {
        [
            (self.max[0] + self.min[0]) / 2.0,
            (self.max[1] + self.min[1]) / 2.0,
            (self.max[2] + self.min[2]) / 2.0,
        ]
    }
}
