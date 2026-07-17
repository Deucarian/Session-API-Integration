# Basic API Integration Usage

Open BasicUsage.unity and enter Play Mode. The sample creates a local Session
service, wraps it in SessionAuthProvider, and supplies that provider to an API
client. Assign an ApiClientConfig before issuing requests.

The fake refresh service keeps the demonstration offline and deterministic.
Production applications should provide their own login and refresh services.
