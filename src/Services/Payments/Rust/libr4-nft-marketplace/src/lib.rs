use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum NFTStatus { Minted, Listed, Sold, Burned }

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct NFT {
    pub id: String,
    pub token_id: String,
    pub creator: String,
    pub owner: String,
    pub metadata_uri: String,
    pub status: NFTStatus,
    pub created_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct NFTListing {
    pub id: String,
    pub nft_id: String,
    pub seller: String,
    pub price: f64,
    pub currency: String,
    pub status: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Royalty {
    pub nft_id: String,
    pub creator: String,
    pub percentage: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct NFTSale {
    pub id: String,
    pub nft_id: String,
    pub from: String,
    pub to: String,
    pub price: f64,
    pub tx_hash: String,
    pub timestamp: u64,
}

impl NFT {
    pub fn new(token_id: String, creator: String, metadata_uri: String) -> Self {
        NFT {
            id: uuid::Uuid::new_v4().to_string(),
            token_id,
            creator: creator.clone(),
            owner: creator,
            metadata_uri,
            status: NFTStatus::Minted,
            created_at: std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap()
                .as_secs(),
        }
    }
}
