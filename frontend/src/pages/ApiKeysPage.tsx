import React, { useEffect, useState } from 'react';
import { api } from '../lib/api';
import styles from './PagesCommon.module.css';

interface ApiKey {
  id: string;
  name: string;
  keyPrefix: string;
  scopes: string[];
  isActive: boolean;
  expiresAt: string | null;
  lastUsedAt: string | null;
  createdAt: string;
}

export const ApiKeysPage: React.FC = () => {
  const [keys, setKeys] = useState<ApiKey[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [newKeyName, setNewKeyName] = useState('');
  const [newKeyScopes, setNewKeyScopes] = useState<string[]>(['logs:read']);
  const [createdKey, setCreatedKey] = useState<string | null>(null);

  useEffect(() => {
    loadKeys();
  }, []);

  const loadKeys = async () => {
    try {
      const response = await api.get('/apikeys');
      setKeys(response.data);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateKey = async () => {
    if (!newKeyName.trim()) {
      alert('Please enter a key name');
      return;
    }

    try {
      const response = await api.post('/apikeys', {
        name: newKeyName,
        scopes: newKeyScopes,
        expiresAt: null
      });
      
      setCreatedKey(response.data.rawKey);
      setShowCreateForm(false);
      setNewKeyName('');
      setNewKeyScopes(['logs:read']);
      await loadKeys();
    } catch (err) {
      alert('Error creating key: ' + (err as Error).message);
    }
  };

  const handleDeactivateKey = async (keyId: string) => {
    if (!confirm('Deactivate this API key?')) return;

    try {
      await api.patch(`/apikeys/${keyId}/deactivate`);
      await loadKeys();
    } catch (err) {
      alert('Error deactivating key: ' + (err as Error).message);
    }
  };

  const handleDeleteKey = async (keyId: string) => {
    if (!confirm('Permanently delete this API key? This cannot be undone.')) return;

    try {
      await api.delete(`/apikeys/${keyId}`);
      await loadKeys();
    } catch (err) {
      alert('Error deleting key: ' + (err as Error).message);
    }
  };

  const toggleScope = (scope: string) => {
    if (newKeyScopes.includes(scope)) {
      setNewKeyScopes(newKeyScopes.filter(s => s !== scope));
    } else {
      setNewKeyScopes([...newKeyScopes, scope]);
    }
  };

  const availableScopes = [
    'logs:read',
    'clusters:read',
    'reports:read',
    'reports:export',
    'uploads:create'
  ];

  if (loading) return <div className={styles.container}>Loading...</div>;

  return (
    <div className={styles.container}>
      <h1>API Keys</h1>
      <p className={styles.subtitle}>Manage API keys for programmatic access</p>

      {createdKey && (
        <div className={styles.alert}>
          <h3>API Key Created</h3>
          <p>Copy this key now. It will not be shown again.</p>
          <code className={styles.keyDisplay}>{createdKey}</code>
          <button onClick={() => setCreatedKey(null)} className={styles.btn}>
            Close
          </button>
        </div>
      )}

      {error && <div className={styles.error}>{error}</div>}

      <button 
        onClick={() => setShowCreateForm(!showCreateForm)} 
        className={styles.btn}
      >
        {showCreateForm ? 'Cancel' : 'Create New Key'}
      </button>

      {showCreateForm && (
        <div className={styles.card}>
          <h3>Create API Key</h3>
          <div className={styles.formGroup}>
            <label>Key Name</label>
            <input
              type="text"
              value={newKeyName}
              onChange={(e) => setNewKeyName(e.target.value)}
              placeholder="e.g., CI/CD Pipeline"
              className={styles.input}
            />
          </div>
          <div className={styles.formGroup}>
            <label>Scopes</label>
            {availableScopes.map(scope => (
              <label key={scope} className={styles.checkboxLabel}>
                <input
                  type="checkbox"
                  checked={newKeyScopes.includes(scope)}
                  onChange={() => toggleScope(scope)}
                />
                {scope}
              </label>
            ))}
          </div>
          <button onClick={handleCreateKey} className={styles.btn}>
            Create Key
          </button>
        </div>
      )}

      <div className={styles.tableContainer}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Name</th>
              <th>Key Prefix</th>
              <th>Scopes</th>
              <th>Status</th>
              <th>Last Used</th>
              <th>Created</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {keys.length === 0 ? (
              <tr>
                <td colSpan={7} className={styles.empty}>No API keys</td>
              </tr>
            ) : (
              keys.map(key => (
                <tr key={key.id}>
                  <td>{key.name}</td>
                  <td><code>{key.keyPrefix}...</code></td>
                  <td>{key.scopes.join(', ')}</td>
                  <td>
                    <span className={key.isActive ? styles.statusActive : styles.statusInactive}>
                      {key.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td>{key.lastUsedAt ? new Date(key.lastUsedAt).toLocaleString() : 'Never'}</td>
                  <td>{new Date(key.createdAt).toLocaleString()}</td>
                  <td>
                    {key.isActive && (
                      <button 
                        onClick={() => handleDeactivateKey(key.id)}
                        className={styles.btnSmall}
                      >
                        Deactivate
                      </button>
                    )}
                    <button 
                      onClick={() => handleDeleteKey(key.id)}
                      className={styles.dangerBtn}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};
