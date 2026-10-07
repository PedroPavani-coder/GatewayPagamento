// Funções compartilhadas pelos scripts de teste de carga (load-test.js e stress-test.js),
// pra não repetir o mesmo código nos dois arquivos.

import http from 'k6/http';
import { check } from 'k6';
import { Rate } from 'k6/metrics';

// Endereço da Api. Dá pra trocar sem mexer no código:  k6 run -e BASE_URL=http://localhost:5000 ...
export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Métrica própria: porcentagem de "passos" do fluxo que falharam (criar ou consultar pedido).
export const taxaDeErros = new Rate('erros_de_fluxo');

// Faz login UMA vez, no começo do teste (a função setup() de cada script chama isso),
// e devolve o token JWT. Todos os usuários virtuais reaproveitam o mesmo token — assim
// o teste mede a criação/consulta de pedidos, e não o endpoint de login.
export function fazerLogin() {
  const resposta = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ userName: 'admin', password: 'admin123' }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  const ok = check(resposta, { 'login retornou 200': (r) => r.status === 200 });
  if (!ok) {
    throw new Error(
      `Login falhou (status ${resposta.status}). A Api está rodando em ${BASE_URL}?`,
    );
  }

  return resposta.json('token');
}

// O que UM usuário virtual faz a cada "rodada": cria um pedido e depois consulta ele.
export function criarEConsultarPedido(token) {
  const params = {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
  };

  // __VU = número do usuário virtual, __ITER = número da rodada dele. Juntos geram
  // um e-mail diferente a cada pedido.
  const corpo = JSON.stringify({
    customerEmail: `carga-${__VU}-${__ITER}@teste.com`,
    items: [
      { productName: 'Produto de carga', quantity: 2, unitPrice: 50 },
      { productName: 'Outro produto', quantity: 1, unitPrice: 30 },
    ],
  });

  const criacao = http.post(`${BASE_URL}/api/orders`, corpo, params);
  const criou = check(criacao, {
    'POST /api/orders retornou 201': (r) => r.status === 201,
  });
  taxaDeErros.add(!criou);

  if (!criou) {
    return;
  }

  const orderId = criacao.json('orderId');
  const consulta = http.get(`${BASE_URL}/api/orders/${orderId}`, params);
  const consultou = check(consulta, {
    'GET /api/orders/{id} retornou 200': (r) => r.status === 200,
  });
  taxaDeErros.add(!consultou);
}
