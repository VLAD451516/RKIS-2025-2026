import { useState, useEffect } from 'react';
import { Container, Row, Col, Card, Form, Pagination, Spinner } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import api from '../api/axios';

const RestaurantList = () => {
  const [restaurants, setRestaurants] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const pageSize = 6;

  useEffect(() => {
    fetchRestaurants();
  }, [page, search]);

  const fetchRestaurants = async () => {
    setLoading(true);
    try {
      const response = await api.get(`/restaurants`, {
        params: { search, page, pageSize }
      });
      setRestaurants(response.data);
      const totalCount = parseInt(response.headers['x-total-count'] || '0');
      setTotalPages(Math.ceil(totalCount / pageSize));
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container className="py-4">
      <h1 className="mb-4">Restaurants</h1>
      <Form.Control
        type="text"
        placeholder="Search restaurants..."
        className="mb-4"
        value={search}
        onChange={(e) => { setSearch(e.target.value); setPage(1); }}
      />
      {loading ? (
        <div className="text-center"><Spinner animation="border" /></div>
      ) : (
        <>
          <Row>
            {restaurants.map(r => (
              <Col key={r.id} md={4} className="mb-4">
                <Card className="h-100">
                  <Card.Img variant="top" src={r.imagePath ? `http://localhost:5137/${r.imagePath}` : 'https://via.placeholder.com/300x200'} />
                  <Card.Body>
                    <Card.Title>{r.name}</Card.Title>
                    <Card.Text>{r.description}</Card.Text>
                    <Link to={`/restaurant/${r.id}`} className="btn btn-primary">View Menu</Link>
                  </Card.Body>
                </Card>
              </Col>
            ))}
          </Row>
          {totalPages > 1 && (
            <Pagination className="justify-content-center">
              {[...Array(totalPages).keys()].map(i => (
                <Pagination.Item key={i + 1} active={i + 1 === page} onClick={() => setPage(i + 1)}>
                  {i + 1}
                </Pagination.Item>
              ))}
            </Pagination>
          )}
        </>
      )}
    </Container>
  );
};

export default RestaurantList;
