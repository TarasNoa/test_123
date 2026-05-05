use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum TokenizableAsset { RealEstate, Artwork, Reputation, Skill }

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TokenizedAsset {
    pub id: String,
    pub asset_type: TokenizableAsset,
    pub owner: String,
    pub total_tokens: f64,
    pub token_symbol: String,
    pub value: f64,
    pub created_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TokenHolder {
    pub user_id: String,
    pub asset_id: String,
    pub tokens_held: f64,
    pub percentage: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ReputationToken {
    pub user_id: String,
    pub total_reputation: f64,
    pub verified_transactions: u32,
    pub average_rating: f64,
}

impl TokenizedAsset {
    pub fn new(
        asset_type: TokenizableAsset,
        owner: String,
        total_tokens: f64,
        token_symbol: String,
        value: f64,
    ) -> Self {
        TokenizedAsset {
            id: uuid::Uuid::new_v4().to_string(),
            asset_type,
            owner,
            total_tokens,
            token_symbol,
            value,
            created_at: std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap()
                .as_secs(),
        }
    }
}
