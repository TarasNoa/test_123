use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum SwapType { ExactInput, ExactOutput }

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SwapOrder {
    pub id: String,
    pub user_id: String,
    pub token_in: String,
    pub token_out: String,
    pub amount_in: f64,
    pub amount_out: f64,
    pub swap_type: SwapType,
    pub status: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct LiquidityPool {
    pub id: String,
    pub token_a: String,
    pub token_b: String,
    pub reserve_a: f64,
    pub reserve_b: f64,
    pub total_shares: f64,
}

impl LiquidityPool {
    pub fn get_price(&self, token: &str) -> f64 {
        if token == self.token_a {
            self.reserve_b / self.reserve_a
        } else {
            self.reserve_a / self.reserve_b
        }
    }

    pub fn swap(&mut self, token_in: &str, amount_in: f64) -> f64 {
        let k = self.reserve_a * self.reserve_b;
        if token_in == self.token_a {
            self.reserve_a += amount_in;
            let new_reserve_b = k / self.reserve_a;
            let amount_out = self.reserve_b - new_reserve_b;
            self.reserve_b = new_reserve_b;
            amount_out
        } else {
            self.reserve_b += amount_in;
            let new_reserve_a = k / self.reserve_b;
            let amount_out = self.reserve_a - new_reserve_a;
            self.reserve_a = new_reserve_a;
            amount_out
        }
    }
}
