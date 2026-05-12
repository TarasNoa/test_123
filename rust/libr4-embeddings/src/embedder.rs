use anyhow::Result;
use ndarray::{Array1, Array2};
use ort::{GraphOptimizationLevel, Session};
use tokenizers::Tokenizer;

pub struct Embedder {
    session: Session,
    tokenizer: Tokenizer,
}

impl Embedder {
    pub fn load(model_path: &str, tokenizer_path: &str) -> Result<Self> {
        let session = Session::builder()?
            .with_optimization_level(GraphOptimizationLevel::All)?
            .with_intra_threads(4)?
            .commit_from_file(model_path)?;

        let tokenizer = Tokenizer::from_file(tokenizer_path)
            .map_err(|e| anyhow::anyhow!("Failed to load tokenizer: {}", e))?;

        Ok(Self { session, tokenizer })
    }

    pub fn embed(&self, text: &str) -> Result<Vec<f32>> {
        let mut batch = self.embed_batch(&[text])?;
        Ok(batch.remove(0))
    }

    pub fn embed_batch(&self, texts: &[&str]) -> Result<Vec<Vec<f32>>> {
        let encodings = self
            .tokenizer
            .encode_batch(
                texts.iter().map(|t| t.to_string()).collect::<Vec<_>>(),
                true,
            )
            .map_err(|e| anyhow::anyhow!("Tokenizer error: {}", e))?;

        let batch_size = texts.len();
        let max_len = encodings.iter().map(|e| e.len()).max().unwrap_or(128).min(512);

        let input_ids: Vec<i64> = encodings
            .iter()
            .flat_map(|e| {
                let mut ids: Vec<i64> = e.get_ids().iter().map(|&x| x as i64).collect();
                ids.resize(max_len, 0);
                ids
            })
            .collect();

        let attention_mask: Vec<i64> = encodings
            .iter()
            .flat_map(|e| {
                let real_len = e.len().min(max_len);
                let mut mask = vec![1i64; real_len];
                mask.resize(max_len, 0);
                mask
            })
            .collect();

        let input_ids_array =
            Array2::from_shape_vec((batch_size, max_len), input_ids)?;
        let attention_mask_array =
            Array2::from_shape_vec((batch_size, max_len), attention_mask)?;

        let outputs = self.session.run(ort::inputs![
            "input_ids" => input_ids_array.view(),
            "attention_mask" => attention_mask_array.view(),
        ]?)?;

        let last_hidden = outputs["last_hidden_state"].try_extract_tensor::<f32>()?;
        let embeddings = mean_pool(last_hidden.view(), &attention_mask_array);

        Ok(embeddings
            .outer_iter()
            .map(|row| l2_normalize(row.to_vec()))
            .collect())
    }
}

fn mean_pool(
    hidden_states: ndarray::ArrayViewD<f32>,
    attention_mask: &Array2<i64>,
) -> Array2<f32> {
    let batch = hidden_states.shape()[0];
    let seq_len = hidden_states.shape()[1];
    let hidden = hidden_states.shape()[2];

    let mut result = Array2::zeros((batch, hidden));
    for b in 0..batch {
        let mut sum = Array1::<f32>::zeros(hidden);
        let mut count = 0f32;
        for s in 0..seq_len {
            if attention_mask[[b, s]] == 1 {
                for h in 0..hidden {
                    sum[h] += hidden_states[[b, s, h]];
                }
                count += 1.0;
            }
        }
        if count > 0.0 {
            for h in 0..hidden {
                result[[b, h]] = sum[h] / count;
            }
        }
    }
    result
}

fn l2_normalize(mut v: Vec<f32>) -> Vec<f32> {
    let norm: f32 = v.iter().map(|x| x * x).sum::<f32>().sqrt();
    if norm > 1e-8 {
        v.iter_mut().for_each(|x| *x /= norm);
    }
    v
}

pub fn cosine_similarity(a: &[f32], b: &[f32]) -> f32 {
    let dot: f32 = a.iter().zip(b.iter()).map(|(x, y)| x * y).sum();
    let na: f32 = a.iter().map(|x| x * x).sum::<f32>().sqrt();
    let nb: f32 = b.iter().map(|x| x * x).sum::<f32>().sqrt();
    let denom = na * nb;
    if denom < 1e-8 { 0.0 } else { dot / denom }
}
