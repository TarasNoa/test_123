use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum OrderType { Market, Limit, StopLoss }

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum OrderStatus { Pending, Filled, Cancelled, Failed }

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ExchangeOrder {
    pub id: String,
    pub user_id: String,
    pub from_currency: String,
    pub to_currency: String,
    pub amount: f64,
    pub order_type: OrderType,
    pub status: OrderStatus,
    pub created_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct OnRampTransaction {
    pub id: String,
    pub user_id: String,
    pub fiat_amount: f64,
    pub crypto_amount: f64,
    pub status: OrderStatus,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct OffRampTransaction {
    pub id: String,
    pub user_id: String,
    pub crypto_amount: f64,
    pub fiat_amount: f64,
    pub status: OrderStatus,
}
