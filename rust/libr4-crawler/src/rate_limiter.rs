use governor::{Quota, RateLimiter, clock::DefaultClock, state::{NotKeyed, InMemoryState}};
use std::collections::HashMap;
use std::num::NonZeroU32;
use std::sync::Arc;
use tokio::sync::RwLock;

type Limiter = Arc<RateLimiter<NotKeyed, InMemoryState, DefaultClock>>;

pub struct DomainRateLimiter {
    limiters: RwLock<HashMap<String, Limiter>>,
}

impl DomainRateLimiter {
    pub fn new() -> Self {
        Self {
            limiters: RwLock::new(HashMap::new()),
        }
    }

    pub async fn wait(&self, domain: &str) {
        let per_second: NonZeroU32 = match domain {
            "hh_ru"    => NonZeroU32::new(1).unwrap(),
            "linkedin" => NonZeroU32::new(1).unwrap(),
            "upwork"   => NonZeroU32::new(2).unwrap(),
            _          => NonZeroU32::new(5).unwrap(),
        };

        let limiter = {
            let read = self.limiters.read().await;
            read.get(domain).cloned()
        };

        let limiter = match limiter {
            Some(l) => l,
            None => {
                let quota = Quota::per_second(per_second);
                let l: Limiter = Arc::new(RateLimiter::direct(quota));
                self.limiters.write().await.insert(domain.to_string(), l.clone());
                l
            }
        };

        limiter.until_ready().await;

        let jitter = rand::random::<u64>() % 400 + 100;
        tokio::time::sleep(tokio::time::Duration::from_millis(jitter)).await;
    }
}
